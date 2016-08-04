namespace bluetoothjx

open System
open System.Threading
open System.Collections.Generic
open System.Text

open Android.App
open Android.Content
open Android.OS
open Android.Runtime
open Android.Views
open Android.Widget
open Android.Webkit
open Android.Bluetooth
open Android.Util
open bluetooth_jx.HtmlBuilder

[<Activity (MainLauncher = true, Icon = "@mipmap/btjx", WindowSoftInputMode = SoftInput.AdjustPan, ScreenOrientation = PM.ScreenOrientation.Portrait )>]
type MainActivity () =
    inherit Activity ()

    let mutable pairedDevices : List<BluetoothDevice> = null
    let mutable bluetoothAdapter = BluetoothAdapter.DefaultAdapter 
    let mutable devicesListView : ListView = null
    let mutable stringBuilder : StringBuilder = new StringBuilder()
    let mutable bluetoothController : BluetoothController = new BluetoothController()
  
   
       
    member public  this.showChat () =
        let view = this.FindViewById<WebView>(Resource_Id.webview)
        this.RunOnUiThread ( fun () -> 
            view.Visibility <- ViewStates.Visible
            view.LoadDataWithBaseURL("", stringBuilder.ToString(), "text/html", "utf-8", "")
        )
   
    member public this.hideChat () =
        let view = this.FindViewById<WebView>(Resource_Id.webview)
        this.RunOnUiThread ( fun () -> view.Visibility <- ViewStates.Gone)

    member public this.AddTextToWebView (text : String) = 
        let webView = this.FindViewById<WebView>(Resource_Id.webview)

        this.RunOnUiThread( fun () ->  
            stringBuilder.AppendLine(BuildHtml(text, false)) |> ignore
            webView.LoadDataWithBaseURL("", stringBuilder.ToString(), "text/html", "utf-8", "")
        )

    override this.OnCreate (bundle) =

        base.OnCreate (bundle)
        this.SetContentView (Resource_Layout.bluetoothactivity)

       
        devicesListView <- this.FindViewById<ListView>(Resource_Id.listView1)
        let adapter = new BluetoothListAdapter(this :> Context, (List.ofSeq (new List<BluetoothDevice>())))
        devicesListView.Adapter <- adapter;

        let button = this.FindViewById<Button>(Resource_Id.turnOn)
        button.Click.Add (fun args -> 
            if bluetoothAdapter.IsEnabled = false then
                let turnOnIntent = new Intent(BluetoothAdapter.ActionRequestEnable)
                this.StartActivityForResult(turnOnIntent,100)
            else
                bluetoothController <- new BluetoothController()
                bluetoothController.SetConnectionCallback(this.showChat, this.hideChat, this.AddTextToWebView)
                bluetoothController.WaitForConnections()
        )

        let listBtn = this.FindViewById<Button>(Resource_Id.paired)
        listBtn.Click.Add( fun args ->
            if bluetoothAdapter.IsEnabled then
                bluetoothController.WaitForConnections() //just in case not waiting...
                pairedDevices <- List<BluetoothDevice>(bluetoothAdapter.BondedDevices)
                let adapter = new BluetoothListAdapter(this :> Context, (List.ofSeq pairedDevices))
                let nameList =  (List.ofSeq pairedDevices) |> List.map (fun x -> x.Address)
                let arrayAdapter : ArrayAdapter = new ArrayAdapter(this, Resource_Layout.TextViewItem, List.toArray nameList)
                devicesListView.Adapter <- arrayAdapter;

                adapter.NotifyDataSetChanged()
            else
                Toast.MakeText(this, "Bluetooth not enabled.", ToastLength.Long).Show() 
        )

        let resetBtn = this.FindViewById<Button>(Resource_Id.resetConnection)
        resetBtn.Click.Add(fun _ ->
            bluetoothController.ResetConnection()
            stringBuilder.Clear() |> ignore
            this.FindViewById<WebView>(Resource_Id.webview).Visibility <- ViewStates.Gone
        )

        devicesListView.ItemClick.Add(fun item ->
           let device = List.nth (List.ofSeq pairedDevices) item.Position
           bluetoothController.TryConnect(device)
           ()
        )

        let sendBtn = this.FindViewById<Button>(Resource_Id.sendbutton)
        sendBtn.Click.Add(fun args ->
            let data = this.FindViewById<EditText>(Resource_Id.sendtext).Text+"\n"
            stringBuilder.AppendLine(BuildHtml(data, true)) |> ignore
            this.FindViewById<WebView>(Resource_Id.webview).LoadDataWithBaseURL("", stringBuilder.ToString(), "text/html", "utf-8", "")
            data |> bluetoothController.Send
        )

        let offBtn = this.FindViewById<Button>(Resource_Id.turnOff)
        offBtn.Click.Add(fun args ->
            if bluetoothAdapter.IsEnabled then
                if bluetoothAdapter.IsDiscovering then
                    bluetoothAdapter.CancelDiscovery() |> ignore
                bluetoothAdapter.Disable() |> ignore
                bluetoothController.ResetConnection()
                bluetoothAdapter <- BluetoothAdapter.DefaultAdapter
        )

        let webView = this.FindViewById<WebView>(Resource_Id.webview)
        webView.LoadDataWithBaseURL("", BuildHtml("", true), "text/html", "utf-8", "")


    override this.OnActivityResult( requestCode, resultCode, data) =
        if requestCode = 100 then
            if bluetoothAdapter.IsEnabled then
                let discoverableIntent :Intent = new Intent(BluetoothAdapter.ActionRequestDiscoverable)
                discoverableIntent.PutExtra(BluetoothAdapter.ExtraDiscoverableDuration, 300) |> ignore

                this.StartActivity(discoverableIntent)
                bluetoothController.WaitForConnections()
            else
                Toast.MakeText(this, "Bluetooth not enabled.", ToastLength.Long).Show() 
    
    member private this.List(view : View) =
        let pairedDevices = bluetoothAdapter.BondedDevices
        ()
