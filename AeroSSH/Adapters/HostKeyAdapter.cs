using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AeroSSH.Adapters;

public class HostKeyAdapter : RecyclerView.Adapter
{
    private readonly Action<string, int> _onForget;
    private List<(string Host, int Port, string Fingerprint)> _items = new();

    public HostKeyAdapter(Action<string, int> onForget) => _onForget = onForget;

    public void Submit(IEnumerable<(string Host, int Port, string Fingerprint)> items)
    {
        _items = items.ToList();
        NotifyDataSetChanged();
    }

    public override int ItemCount => _items.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var view = LayoutInflater.From(parent.Context)!.Inflate(Resource.Layout.item_host_key, parent, false)!;
        return new Vh(view);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var vh = (Vh)holder;
        var item = _items[position];
        vh.HostPort.Text = $"{item.Host}:{item.Port}";
        vh.Fingerprint.Text = item.Fingerprint;
        vh.Forget.Click -= vh.OnForgetClick;
        vh.OnForgetClick = (_, _) => _onForget(item.Host, item.Port);
        vh.Forget.Click += vh.OnForgetClick;
    }

    private class Vh : RecyclerView.ViewHolder
    {
        public TextView HostPort { get; }
        public TextView Fingerprint { get; }
        public ImageButton Forget { get; }
        public EventHandler OnForgetClick = (_, _) => { };

        public Vh(View view) : base(view)
        {
            HostPort = view.FindViewById<TextView>(Resource.Id.hostKeyHostPort)!;
            Fingerprint = view.FindViewById<TextView>(Resource.Id.hostKeyFingerprint)!;
            Forget = view.FindViewById<ImageButton>(Resource.Id.btnForgetHostKey)!;
        }
    }
}
