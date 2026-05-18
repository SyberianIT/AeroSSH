using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace AeroSSH.Services;

[Service(
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeDataSync)]
public class SshForegroundService : Service
{
    public const string ChannelId = "aerossh_session";
    public const int NotificationId = 4711;

    public const string ActionStart = "io.github.syberianit.aerossh.START";
    public const string ActionStop = "io.github.syberianit.aerossh.STOP";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ActionStop)
        {
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        EnsureChannel();
        var notification = BuildNotification(intent?.GetStringExtra("title") ?? GetString(Resource.String.notif_default_title)!);

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
            StartForeground(NotificationId, notification, ForegroundService.TypeDataSync);
        else
            StartForeground(NotificationId, notification);

        return StartCommandResult.Sticky;
    }

    private Notification BuildNotification(string title)
    {
        var stopIntent = new Intent(this, typeof(SshForegroundService)).SetAction(ActionStop);
        var stopFlags = OperatingSystem.IsAndroidVersionAtLeast(31)
            ? PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent
            : PendingIntentFlags.UpdateCurrent;
        var stopPending = PendingIntent.GetService(this, 0, stopIntent, stopFlags);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle(title)
            .SetContentText(GetString(Resource.String.notif_session_text))
            .SetSmallIcon(Resource.Drawable.ic_notification)
            .SetOngoing(true)
            .SetCategory(NotificationCompat.CategoryService)
            .SetPriority(NotificationCompat.PriorityLow)
            .AddAction(0, GetString(Resource.String.notif_stop)!, stopPending)
            .Build();
    }

    private void EnsureChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;
        var manager = (NotificationManager)GetSystemService(NotificationService)!;
        if (manager.GetNotificationChannel(ChannelId) != null) return;

        var channel = new NotificationChannel(
            ChannelId,
            GetString(Resource.String.notif_channel_name),
            NotificationImportance.Low)
        {
            Description = GetString(Resource.String.notif_channel_desc)
        };
        manager.CreateNotificationChannel(channel);
    }

    public static void Start(Context context, string title)
    {
        var intent = new Intent(context, typeof(SshForegroundService))
            .SetAction(ActionStart)
            .PutExtra("title", title);
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
    }

    public static void Stop(Context context)
    {
        var intent = new Intent(context, typeof(SshForegroundService)).SetAction(ActionStop);
        context.StartService(intent);
    }
}
