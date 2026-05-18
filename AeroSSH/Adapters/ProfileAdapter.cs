using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AeroSSH.Models;

namespace AeroSSH.Adapters;

public class ProfileAdapter : RecyclerView.Adapter
{
    private readonly Action<ServerProfile> _onClick;
    private readonly Action<ServerProfile> _onEdit;
    private readonly Action<ServerProfile> _onDelete;
    private List<ServerProfile> _items = new();

    public ProfileAdapter(Action<ServerProfile> onClick, Action<ServerProfile> onEdit, Action<ServerProfile> onDelete)
    {
        _onClick = onClick;
        _onEdit = onEdit;
        _onDelete = onDelete;
    }

    public void Submit(IEnumerable<ServerProfile> profiles)
    {
        _items = profiles.ToList();
        NotifyDataSetChanged();
    }

    public override int ItemCount => _items.Count;

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var view = LayoutInflater.From(parent.Context)!.Inflate(Resource.Layout.item_profile, parent, false)!;
        return new Vh(view);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        var vh = (Vh)holder;
        var item = _items[position];
        vh.Name.Text = item.DisplayLabel;
        vh.Detail.Text = $"{item.Username}@{item.Host}:{item.Port} · {item.AuthMethod}";
        vh.ItemView.Click -= vh.OnClick;
        vh.Edit.Click -= vh.OnEditClick;
        vh.Delete.Click -= vh.OnDeleteClick;
        vh.OnClick = (_, _) => _onClick(item);
        vh.OnEditClick = (_, _) => _onEdit(item);
        vh.OnDeleteClick = (_, _) => _onDelete(item);
        vh.ItemView.Click += vh.OnClick;
        vh.Edit.Click += vh.OnEditClick;
        vh.Delete.Click += vh.OnDeleteClick;
    }

    private class Vh : RecyclerView.ViewHolder
    {
        public TextView Name { get; }
        public TextView Detail { get; }
        public ImageButton Edit { get; }
        public ImageButton Delete { get; }
        public EventHandler OnClick = (_, _) => { };
        public EventHandler OnEditClick = (_, _) => { };
        public EventHandler OnDeleteClick = (_, _) => { };

        public Vh(View view) : base(view)
        {
            Name = view.FindViewById<TextView>(Resource.Id.profileName)!;
            Detail = view.FindViewById<TextView>(Resource.Id.profileDetail)!;
            Edit = view.FindViewById<ImageButton>(Resource.Id.btnEditProfile)!;
            Delete = view.FindViewById<ImageButton>(Resource.Id.btnDeleteProfile)!;
        }
    }
}
