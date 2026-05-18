using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AeroSSH.Models;

namespace AeroSSH.Adapters;

public class SftpEntryAdapter : RecyclerView.Adapter
{
    private readonly Action<SftpEntry> _onClick;
    private readonly Action<SftpEntry> _onLongClick;
    private List<SftpEntry> _items = new();

    public SftpEntryAdapter(Action<SftpEntry> onClick, Action<SftpEntry> onLongClick)
    {
        _onClick = onClick;
        _onLongClick = onLongClick;
    }

    public void Submit(IEnumerable<SftpEntry> items)
    {
        _items = items.ToList();
        NotifyDataSetChanged();
    }

    public override int ItemCount => _items.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var view = LayoutInflater.From(parent.Context)!.Inflate(Resource.Layout.item_sftp_entry, parent, false)!;
        return new Vh(view);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var vh = (Vh)holder;
        var item = _items[position];
        vh.Icon.SetImageResource(item.IsDirectory ? Resource.Drawable.ic_folder : Resource.Drawable.ic_file);
        vh.Name.Text = item.Name;
        vh.Detail.Text = item.IsDirectory ? "—" : FormatBytes(item.Length);

        vh.ItemView.Click -= vh.OnClick;
        vh.ItemView.LongClick -= vh.OnLongClick;
        vh.OnClick = (_, _) => _onClick(item);
        vh.OnLongClick = (_, e) => { _onLongClick(item); e.Handled = true; };
        vh.ItemView.Click += vh.OnClick;
        vh.ItemView.LongClick += vh.OnLongClick;
    }

    private static string FormatBytes(long bytes)
    {
        string[] u = { "B", "KB", "MB", "GB", "TB" };
        double s = bytes;
        var i = 0;
        while (s >= 1024 && i < u.Length - 1) { s /= 1024; i++; }
        return $"{s:0.##} {u[i]}";
    }

    private class Vh : RecyclerView.ViewHolder
    {
        public ImageView Icon { get; }
        public TextView Name { get; }
        public TextView Detail { get; }
        public EventHandler OnClick = (_, _) => { };
        public EventHandler<View.LongClickEventArgs> OnLongClick = (_, _) => { };

        public Vh(View view) : base(view)
        {
            Icon = view.FindViewById<ImageView>(Resource.Id.sftpIcon)!;
            Name = view.FindViewById<TextView>(Resource.Id.sftpName)!;
            Detail = view.FindViewById<TextView>(Resource.Id.sftpDetail)!;
        }
    }
}
