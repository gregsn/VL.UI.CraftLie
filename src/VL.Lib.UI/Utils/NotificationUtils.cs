﻿using SharpDX;
using System;
using VL.Lib.UI.Notifications;

namespace VL.Lib.UI
{
    public class NotificationTransformer
    {
        readonly Vector2 Offset;
        readonly Vector2 Scaling;

        public NotificationTransformer(Vector2 offset, Vector2 scaling)
        {
            Offset = offset;
            Scaling = scaling;
        }

        public Vector2 Transform(Vector2 point)
        {
            return new Vector2(point.X * Scaling.X + Offset.X, point.Y * Scaling.Y + Offset.Y);
        }      
    }

    public static class NotificationUtils
    {
        public static TResult MouseKeyboardSwitch<TResult>(object eventArg, TResult defaultResult,
            Func<MouseDownNotification, TResult> onMouseDown = null,
            Func<MouseMoveNotification, TResult> onMouseMove = null,
            Func<MouseUpNotification, TResult> onMouseUp = null,
            Func<KeyDownNotification, TResult> onKeyDown = null,
            Func<KeyUpNotification, TResult> onKeyUp = null,
            Func<KeyPressNotification, TResult> onKeyPress = null)
        {
            return NotificationSwitch(eventArg, defaultResult,
                mn => MouseNotificationSwitch(mn, defaultResult, onMouseDown, onMouseMove, onMouseUp),
                kn => KeyNotificationSwitch(kn, defaultResult, onKeyDown, onKeyUp, onKeyPress));
        }

        public static object TransformNotification(object notification, NotificationTransformer transformer)
        {
            return NotificationSwitch(notification, notification,

                //mouse
                mn => MouseNotificationSwitch(mn, notification,
                    n => new MouseDownNotification(
                        transformer.Transform(n.Position),
                        transformer.Transform(n.ClientArea),
                        n.Buttons),

                    n => new MouseMoveNotification(
                        transformer.Transform(n.Position),
                        transformer.Transform(n.ClientArea)),

                    n => new MouseUpNotification(
                        transformer.Transform(n.Position),
                        transformer.Transform(n.ClientArea),
                        n.Buttons)
                ),
                null, //keyboard
                null, //touch
                null); //gesture
        }

        public static IUIHandler DragMouseHandler(
                Action<MouseDownNotification> onDragStart,
                Action<MouseMoveNotification, Vector2> onDrag,
                Action<MouseNotification> onDragEnd)
        {
            return new MouseHandlerAdapter(Handler.DragMouseHandler(onDragStart, onDrag, onDragEnd));
        }

        public static IUIHandler SelectionRectHandler(
                Action<MouseDownNotification> onMouseDown,
                Action<MouseDownNotification> onSelectionStart,
                Action<MouseMoveNotification, RectangleF> onSelection,
                Action<MouseNotification> onSelectionFinish)
        {
            return new MouseHandlerAdapter(Handler.SelectionRectMouseHandler(onMouseDown, onSelectionStart, onSelection, onSelectionFinish));
        }

        public static TResult PositionEvent<TResult>(object eventArg, TResult defaultResult,
                Func<Vector2, TResult> onPositionEvent = null)
                where TResult : class
        {
            return NotificationSwitch(eventArg, defaultResult,
                mn => onPositionEvent(mn.Position),
                null,
                tn => onPositionEvent(tn.Position),
                gn => onPositionEvent(gn.Position));
        }

        public static TResult NotificationSwitch<TResult>(object eventArg, TResult defaultResult, 
            Func<MouseNotification, TResult> mouseFunc = null,
            Func<KeyNotification, TResult> keyFunc = null,
            Func<TouchNotification, TResult> touchFunc = null,
            Func<GestureNotification, TResult> gestureFunc = null)
        {
            if (mouseFunc != null)
            {
                var mouseNotification = eventArg as MouseNotification;
                if (mouseNotification != null)
                {
                    return mouseFunc(mouseNotification);
                } 
            }

            if (touchFunc != null)
            {
                var touchNotification = eventArg as TouchNotification;
                if (touchNotification != null)
                {
                    return touchFunc(touchNotification);
                } 
            }

            if (keyFunc != null)
            {
                var keyNotification = eventArg as KeyNotification;
                if (keyNotification != null)
                {
                    return keyFunc(keyNotification);
                } 
            }

            if (gestureFunc != null)
            {
                var gestureNotification = eventArg as GestureNotification;
                if (gestureNotification != null)
                {
                    return gestureFunc(gestureNotification);
                } 
            }

            return defaultResult;
        }

        public static TResult MouseNotificationSwitch<TResult>(MouseNotification notification, TResult defaultResult,
            Func<MouseDownNotification, TResult> onDown = null,
            Func<MouseMoveNotification, TResult> onMove = null,
            Func<MouseUpNotification, TResult> onUp = null,
            Func<MouseClickNotification, TResult> onClick = null,
            Func<MouseWheelNotification, TResult> onWheel = null,
            Func<MouseHorizontalWheelNotification, TResult> onHorizontalWheel = null)
        {
            switch (notification.Kind)
            {
                case MouseNotificationKind.MouseDown:
                    return onDown != null ? onDown((MouseDownNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseUp:
                    return onUp != null ? onUp((MouseUpNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseMove:
                    return onMove != null ? onMove((MouseMoveNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseWheel:
                    return onWheel != null? onWheel((MouseWheelNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseHorizontalWheel:
                    return onHorizontalWheel != null ? onHorizontalWheel((MouseHorizontalWheelNotification)notification) : defaultResult;
                case MouseNotificationKind.MouseClick:
                    return onClick != null ? onClick((MouseClickNotification)notification) : defaultResult;
                default:
                    return defaultResult;
            }
        }

        public static TResult KeyNotificationSwitch<TResult>(KeyNotification notification, TResult defaultResult,
            Func<KeyDownNotification, TResult> onDown = null,
            Func<KeyUpNotification, TResult> onUp = null,
            Func<KeyPressNotification, TResult> onPress = null,
            Func<KeyboardLostNotification, TResult> onDeviceLost = null)
        {
            switch (notification.Kind)
            {
                case KeyNotificationKind.KeyDown:
                    return onDown != null ? onDown((KeyDownNotification)notification) : defaultResult;
                case KeyNotificationKind.KeyPress:
                    return onPress != null ? onPress((KeyPressNotification)notification) : defaultResult;
                case KeyNotificationKind.KeyUp:
                    return onUp != null ? onUp((KeyUpNotification)notification) : defaultResult;
                case KeyNotificationKind.DeviceLost:
                    return onDeviceLost != null ? onDeviceLost((KeyboardLostNotification)notification) : defaultResult;
                default:
                    return defaultResult;
            }
        }

        static bool SafeCall<TIn, TCast>(TIn value, Action<TCast> action)
            where TCast : class
        {
            if (action != null)
            {
                var cast = value as TCast;
                if (cast != null)
                {
                    action(cast);
                    return true;
                }
            }

            return false;
        }
    }
}
