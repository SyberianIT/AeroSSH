using Android.Graphics;
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
        vh.Tag.Text = $"{TagFor(item.Level)}/{TagFor(item.Source)}";
        vh.Message.Text = item.Message;

        var ctx = vh.ItemView.Context!;
        var color = ColorFor(ctx, item);
        vh.Accent.SetBackgroundColor(color);
        vh.Tag.SetTextColor(color);
    }

    private static string TagFor(LogLevel level) => level switch
    {
        LogLevel.Debug => "DBG",
        LogLevel.Info => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        _ => level.ToString().ToUpperInvariant()
    };

    private static string TagFor(LogSource source) => source switch
    {
        LogSource.System => "SYS",
        LogSource.Stdout => "OUT",
        LogSource.Stderr => "ERR",
        LogSource.Command => "CMD",
        _ => source.ToString().ToUpperInvariant()
    };

    private static Color ColorFor(Android.Content.Context ctx, LogEntry entry)
    {
        var res = entry.Level switch
        {
            LogLevel.Error => Resource.Color.log_error,
            LogLevel.Warning => Resource.Color.log_warning,
            LogLevel.Debug => Resource.Color.log_debug,
            _ => entry.Source switch
            {
                LogSource.Stderr => Resource.Color.log_error,
                LogSource.Stdout => Resource.Color.log_stdout,
                LogSource.Command => Resource.Color.log_command,
                _ => Resource.Color.log_info
            }
        };
        return new Color(AndroidX.Core.Content.ContextCompat.GetColor(ctx, res));
    }

    private class Vh : RecyclerView.ViewHolder
    {
        public TextView Timestamp { get; }
        public TextView Tag { get; }
        public TextView Message { get; }
        public View Accent { get; }

        public Vh(View view) : base(view)
        {
            Timestamp = view.FindViewById<TextView>(Resource.Id.logTimestamp)!;
            Tag = view.FindViewById<TextView>(Resource.Id.logTag)!;
            Message = view.FindViewById<TextView>(Resource.Id.logMessage)!;
            Accent = view.FindViewById<View>(Resource.Id.logAccent)!;
        }
    }
}
