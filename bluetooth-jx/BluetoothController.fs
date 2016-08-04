namespace bluetoothjx
open System
open System.Collections.Generic
open System.IO
open Java.Util
open Android.Bluetooth
open Android.Util
open Android.App
open FSharp.Core
open System.Threading

type BluetoothState = Idle | Waiting | Connecting | Connected

(* Bluetooth controller, controls three main bluetooth actions - *)
(* 1. waiting for a connection to come in                        *)
(* 2. trying to connect to a remote (paired) device              *)
(* 3. handling an open connection and sending/receiving data     *)
(*                                                               *)
type BluetoothController()= 
    let mutable state : BluetoothState = Idle 
    let mutable connection : BluetoothSocket = null
    let mutable sendQ : Queue<string>  = new Queue<string>()
   
    let mutable onConnectionOpened = fun () -> ()
    let mutable onConnectionClosed = fun () -> ()
    let mutable onMessage = fun (msg : String) -> ()

    let mutable reader : StreamReader = null
    let mutable writer : StreamWriter = null

    member private this.bUUID = UUID.FromString("8447f422-f93c-4de0-aa43-5f80f3219217")
    member private this.bluetoothAdapter : BluetoothAdapter = BluetoothAdapter.DefaultAdapter
    member private this.locker = new Object();


    member public this.SetConnectionCallback(openCallback, closeCallback, onMessageReceived) =
        onConnectionOpened <- openCallback
        onConnectionClosed <- closeCallback
        onMessage <- onMessageReceived

    (* Sends a message to the remote device.         *)
    (* The message will be added to a message queue, *)
    (* and sent at the next send cycle.              *)
    member public this.Send(msg : string) =
        Monitor.Enter this.locker
        try
            sendQ.Enqueue(msg)
        finally
            Monitor.Exit this.locker



    (* Resets the connection to Idle.           *)
    (* This will cause the async tasks to stop. *)
    member public this.ResetConnection() =

        onConnectionClosed()
        Monitor.Enter this.locker
        try
            if reader <> null then reader.Close()
            if writer <> null then writer.Close()
        with | anyEx ->
             Log.Verbose("Bluetooth-jx","Exception closing streams. "+anyEx.Message) |> ignore
        if this.IsConnected() then connection.Close()
        state <- Idle
        Monitor.Exit this.locker

    

    member private this.IsConnected() =
        connection <> null && connection.IsConnected

    (* Begins the bluetooth network connection. This means *)
    (* the connection variable is an active connection.    *)
    (* Two async loops are created for the outputstream    *)
    (* and inputstream, respectively.                      *)
    member public this.BeginConnection() = 
        onConnectionOpened()
        reader <- new StreamReader(connection.InputStream)
        let rec AsyncStreamReader() = async {
            Monitor.Enter this.locker
            try
                if true then
                    try
                        let line = reader.ReadLine()
                        if line <> null then
                            onMessage line
                    with 
                        | anyEx -> 
                            Log.Verbose("Bluetooth-jx","Exception in StreamReader. "+anyEx.Message) |> ignore
                            this.ResetConnection()
                    do! Async.Sleep(100)
                    return! AsyncStreamReader()
                else 
                    reader.Close()
                    return ()
            finally 
                Monitor.Exit this.locker
            
        } 
        Async.Start(AsyncStreamReader())

        writer <- new StreamWriter(connection.OutputStream)
        let rec AsyncStreamWriter() = async {

            Monitor.Enter this.locker
            try
                if true then //state = Connected && this.IsConnected() then
                    try
                        if sendQ.Count > 0 then
                            let nextmsg = sendQ.Dequeue()
                            if nextmsg.Length > 0 then
                                let data = System.Text.Encoding.UTF8.GetBytes(nextmsg)
                                connection.OutputStream.Write(data, 0,data.Length);
                    with 
                        | anyEx -> 
                            Log.Verbose("Bluetooth-jx","Exception in StreamWriter. "+anyEx.Message) |> ignore
                            this.ResetConnection()
                    do! Async.Sleep(120)
                    return! AsyncStreamWriter()
                else 
                    writer.Close()
                    return ()
            finally
                Monitor.Exit this.locker
        } 
        Async.Start(AsyncStreamWriter())
      


    (* Wait for connections *)
    member public this.WaitForConnections() =
        Monitor.Enter this.locker
        match state with
            | Connected -> ()
            | Waiting -> ()
            | _ ->
                state <- Waiting
                let rec asyncWait() = async {
                    Monitor.Enter this.locker
                    try
                        let bss = this.bluetoothAdapter.ListenUsingRfcommWithServiceRecord("F#", this.bUUID)
                        if state <> Connected then
                            try
                                connection <- bss.Accept()
                                ()
                            with
                                | anyEx -> Log.Verbose("Bluetooth-jx","Exception waiting for connection. "+anyEx.Message) |> ignore

                            if connection <> null then

                                    match state with
                                    | Connected -> 
                                        connection <- connection
                                        this.BeginConnection()
                                        ()
                                    | _ -> 
                                        state <- Connected
                                        connection <- connection
                                        this.BeginConnection()
                                        ()
                    finally
                        Monitor.Exit this.locker

                    do! Async.Sleep(400)
                    if state = Connected then
                        return ()
                    else if state = Idle then
                        return ()
                    else
                        return! asyncWait()
                }
                Async.Start(asyncWait())
        Monitor.Exit this.locker     


    (* Tries to connect to remote bluetooth device. *)
    member public this.TryConnect (device : BluetoothDevice) =
        Monitor.Enter this.locker
        if state <> Connected then
            state <- Connecting
            //try to connect...
            let asyncTryConnect = async {
                Monitor.Enter this.locker

                try
                    match state with 
                    | Idle -> 
                        return ()
                    | Connected -> ()
                    | _ -> 
                        this.bluetoothAdapter.CancelDiscovery() |> ignore
                        let bss = device.CreateRfcommSocketToServiceRecord(this.bUUID)
                        try
                            bss.Connect()
                            connection <- bss
                            this.BeginConnection()
                            state <- Connected
                        with
                            | anyEx -> 
                                Log.Verbose("Bluetooth-jx","Exception trying to connect. "+anyEx.Message) |> ignore
                                state <- Connecting
                                try
                                    connection.Close()
                                with
                                    | anyEx -> 
                                        Log.Verbose("Bluetooth-jx","Exception closing connection. "+anyEx.Message) |> ignore
                        ()
                finally
                    Monitor.Exit this.locker
            }
            Async.Start(asyncTryConnect)
        Monitor.Exit this.locker
