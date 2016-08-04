

namespace bluetoothjx
open System
open System.Collections.Generic
open Android.Bluetooth
open Android.Widget
open Android.Content
open Android.Views
open Android.Util

type ViewHolder (tv : TextView) =
    member public this.textView = tv

type BluetoothListAdapter(context : Context, bluetoothDeviceList : BluetoothDevice list) = 
    inherit BaseAdapter()

    member private this.bluetoothDeviceList = bluetoothDeviceList
    member private this.inflater : LayoutInflater = LayoutInflater.From(context) 

    override this.GetItem(position) =
        null

    override this.GetItemId(position) =
        1L

    override this.get_Count() =
        List.length this.bluetoothDeviceList



    override this.GetView(position, convertView, parent) =
        let mutable view = convertView
        if view = null then
            let view : View = this.inflater.Inflate(Android.Resource.Layout.SimpleListItem1, null)
            let device = (List.nth this.bluetoothDeviceList position)
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text <- device.Address
            view
            
        else
            let device = (List.nth this.bluetoothDeviceList position)
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text <- device.Address
            view





