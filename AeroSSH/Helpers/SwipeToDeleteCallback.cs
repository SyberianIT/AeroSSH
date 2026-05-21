using Android.Graphics;
using Android.Graphics.Drawables;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;

namespace AeroSSH.Helpers;

public class SwipeToDeleteCallback : ItemTouchHelper.SimpleCallback
{
    private readonly Action<int> _onSwiped;
    private readonly Color _bg;
    private readonly Drawable? _icon;
    private readonly int _iconMargin;

    public SwipeToDeleteCallback(Android.Content.Context context, Action<int> onSwiped)
        : base(0, ItemTouchHelper.Left | ItemTouchHelper.Right)
    {
        _onSwiped = onSwiped;
        _bg = new Color(ContextCompat.GetColor(context, Resource.Color.log_error));
        _icon = ContextCompat.GetDrawable(context, Resource.Drawable.ic_delete);
        _iconMargin = (int)(16 * context.Resources!.DisplayMetrics!.Density);
    }

    public override bool OnMove(RecyclerView p0, RecyclerView.ViewHolder p1, RecyclerView.ViewHolder p2) => false;

    public override void OnSwiped(RecyclerView.ViewHolder viewHolder, int direction)
    {
        _onSwiped(viewHolder.BindingAdapterPosition);
    }

    public override void OnChildDraw(Canvas c, RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder,
        float dX, float dY, int actionState, bool isCurrentlyActive)
    {
        var item = viewHolder.ItemView;
        var paint = new Paint { Color = _bg };

        if (dX > 0)
        {
            c.DrawRect(item.Left, item.Top, item.Left + dX, item.Bottom, paint);
            if (_icon != null)
            {
                var top = item.Top + (item.Height - _icon.IntrinsicHeight) / 2;
                _icon.SetBounds(item.Left + _iconMargin, top,
                    item.Left + _iconMargin + _icon.IntrinsicWidth, top + _icon.IntrinsicHeight);
                _icon.Draw(c);
            }
        }
        else if (dX < 0)
        {
            c.DrawRect(item.Right + dX, item.Top, item.Right, item.Bottom, paint);
            if (_icon != null)
            {
                var top = item.Top + (item.Height - _icon.IntrinsicHeight) / 2;
                _icon.SetBounds(item.Right - _iconMargin - _icon.IntrinsicWidth, top,
                    item.Right - _iconMargin, top + _icon.IntrinsicHeight);
                _icon.Draw(c);
            }
        }

        base.OnChildDraw(c, recyclerView, viewHolder, dX, dY, actionState, isCurrentlyActive);
    }
}
