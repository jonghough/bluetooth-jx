# Bluetooth-jx

This is a sample bluetooth chat project for *Android* + *Xamarin*, written in *F#*. 

The purpose of the project is to evaluate and learn Xamarin, as well as evaluating F#'s suitability for writing general Android Xamarin/Android apps.

The app itself is basically a copy of the *Android sample bluetooth application*, rewritten in F# and with a slightly different UI.

# Points to note
* The app will search for paired devices and list them, it will not pair devices itself. (Uses the phone's bluetooth functionality to pre-pair devices).
* The app simply allows two devices to send messages to each other and displays the chat history in a *webview*.
* The app makes use of F#'s `async` functionality to wait for connections, attempt connections, keep connections alive.
* The programming style is essentially imperative, with lots of mutable state.