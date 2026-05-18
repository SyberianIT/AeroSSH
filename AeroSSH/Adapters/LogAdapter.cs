using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AeroSSH.Models;

namespace AeroSSH.Adapters;

public class LogAdapter : RecyclerView.Adapter
{
    private List<LogEntry> _items = new();

    public void Submit(IEnumerable<LogEntry> items)
    {
        _items = items.ToList();
        NotifyDataSetChanged();
    }

    public override int ItemCount => _items.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var view = LayoutInflater.From(parent.Context)!.Inflate(Resource.Layout.item_log, parent, false)!;
        return new Vh(view);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var vh = (Vh)holder;
        var item = _items[position];
        vh.Timestamp.Text = item.Timestamp.ToLocalTime().ToString("HH:mm:ss");
        vh.Tag.Text = $"{item.Level}/{item.Source}";
        vh.Message.Text = item.Message;
    }

    private class Vh : RecyclerView.ViewHolder
    {
        public TextView Timestamp { get; }
        public TextView Tag { get; }
        public TextView Message { get; }

        public Vh(View view) : base(view)
        {
            Timestamp = view.FindViewById<TextView>(Resource.Id.logTimestamp)!;
            Tag = view.FindViewById<TextView>(Resource.Id.logTag)!;
            Message = view.FindViewById<TextView>(Resource.Id.logMessage)!;
        }
    }
}
