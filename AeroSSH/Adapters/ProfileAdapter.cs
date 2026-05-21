using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AeroSSH.Models;

namespace AeroSSH.Adapters;

public class ProfileAdapter : RecyclerView.Adapter
{
    private readonly Action<ServerProfile> _onClick;
    private readonly Action<ServerProfile> _onEdit;
    private List<ServerProfile> _items = new();

    public ProfileAdapter(Action<ServerProfile> onClick, Action<ServerProfile> onEdit)
    {
        _onClick = onClick;
        _onEdit = onEdit;
    }

    public IReadOnlyList<ServerProfile> Items => _items;

    public void Submit(IEnumerable<ServerProfile> profiles)
    {
        _items = profiles.ToList();
        NotifyDataSetChanged();
    }

    public ServerProfile RemoveAt(int position)
    {
        var item = _items[position];
        _items.RemoveAt(position);
        NotifyItemRemoved(position);
        return item;
    }

    public void Insert(int position, ServerProfile item)
    {
        _items.Insert(position, item);
        NotifyItemInserted(position);
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

        vh.Initial.Text = (string.IsNullOrEmpty(item.Name) ? item.Host : item.Name)
            .TrimStart()
            .Substring(0, 1)
            .ToUpperInvariant();
        vh.Name.Text = item.DisplayLabel;
        vh.Detail.Text = $"{item.Username}@{item.Host}:{item.Port} · {item.AuthMethod}";
        vh.LastUsed.Text = FormatRelative(item.LastUsedAt);

        vh.ItemView.Click -= vh.OnClick;
        vh.Edit.Click -= vh.OnEditClick;
        vh.OnClick = (_, _) => _onClick(item);
        vh.OnEditClick = (_, _) => _onEdit(item);
        vh.ItemView.Click += vh.OnClick;
        vh.Edit.Click += vh.OnEditClick;
    }

    private static string FormatRelative(DateTime when)
    {
        var diff = DateTime.UtcNow - when;
        if (diff.TotalSeconds < 60) return "Только что";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} мин назад";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} ч назад";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} дн назад";
        return when.ToLocalTime().ToString("d MMM yyyy");
    }

    private class Vh : RecyclerView.ViewHolder
    {
        public TextView Initial { get; }
        public TextView Name { get; }
        public TextView Detail { get; }
        public TextView LastUsed { get; }
        public ImageButton Edit { get; }
        public EventHandler OnClick = (_, _) => { };
        public EventHandler OnEditClick = (_, _) => { };

        public Vh(View view) : base(view)
        {
            Initial = view.FindViewById<TextView>(Resource.Id.profileInitial)!;
            Name = view.FindViewById<TextView>(Resource.Id.profileName)!;
            Detail = view.FindViewById<TextView>(Resource.Id.profileDetail)!;
            LastUsed = view.FindViewById<TextView>(Resource.Id.profileLastUsed)!;
            Edit = view.FindViewById<ImageButton>(Resource.Id.btnEditProfile)!;
        }
    }
}
