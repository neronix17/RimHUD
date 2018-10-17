﻿using System.Collections;
using System.Linq;
using RimHUD.Patch;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimHUD.Interface
{
    internal static class GUIPlus
    {
        public const int TooltipId = 10001000;
        public const float ScrollbarWidth = 20f;
        public const float TinyPadding = 2f;
        public const float SmallPadding = 4f;
        public const float MediumPadding = 8f;
        public const float LargePadding = 16f;

        private static readonly Stack SavedColors = new Stack();
        private static readonly Stack SavedFonts = new Stack();

        public static void SetColor(Color? color)
        {
            if (color == null)
            {
                SavedColors.Push(null);
                return;
            }

            SavedColors.Push((Color?) GUI.color);

            GUI.color = color.Value;
        }
        public static void ResetColor()
        {
            var color = (Color?) SavedColors.Pop();
            if (color == null) { return; }

            GUI.color = color.Value;
        }

        public static void SetEnabledColor(bool enabled) => SetColor(enabled ? (Color?) null : Theme.DisabledColor.Value);

        public static void SetFont(GameFont? font)
        {
            if (font == null)
            {
                SavedFonts.Push(null);
                return;
            }

            SavedFonts.Push((GameFont?) Text.Font);

            Text.Font = font.Value;
        }

        public static void ResetFont()
        {
            var font = (GameFont?) SavedFonts.Pop();
            if (font == null) { return; }

            Text.Font = font.Value;
        }

        public static void DrawText(Rect rect, string text, Color? color = null, TextStyle style = null, TipSignal? tooltip = null)
        {
            if (text.NullOrEmpty()) { return; }

            SetColor(color);
            var textRect = rect;
            var textStyle = style?.GUIStyle ?? Text.CurFontStyle;
            var content = new GUIContent(text);
            if (textStyle.CalcSize(content).x > rect.width)
            {
                content.text = "...";
                var ellipsesLength = textStyle.CalcSize(content);
                textRect.width -= ellipsesLength.x;
                GUI.Label(new Rect(rect.RightPartPixels(ellipsesLength.x)), content.text, textStyle);
                if (tooltip == null) { tooltip = new TipSignal(text.Size(textStyle.fontSize)); }
            }

            GUI.Label(textRect, text, textStyle);
            DrawTooltip(rect, tooltip, false);
            ResetColor();
        }

        public static bool DrawButton(Rect rect, Texture2D texture, TipSignal? tooltip = null, bool enabled = true)
        {
            SetEnabledColor(enabled);
            var result = Widgets.ButtonImage(rect, texture);
            ResetColor();

            DrawTooltip(rect, tooltip, false);

            return result & enabled;
        }

        public static bool DrawButton(Rect rect, string label, TipSignal? tooltip = null, bool enabled = true)
        {
            SetEnabledColor(enabled);
            var result = Widgets.ButtonText(rect, label, active: enabled);
            DrawTooltip(rect, tooltip, true);
            ResetColor();

            return result & enabled;
        }

        public static int DrawButtonRow(Rect rect, float width, float padding, params string[] labels)
        {
            var grid = rect.GetHGrid(padding, Enumerable.Repeat(width, labels.Length).ToArray());

            for (var index = 0; index < labels.Length; index++)
            {
                if (DrawButton(grid[index + 1], labels[index])) { return index + 1; }
            }

            return 0;
        }

        public static bool DrawToggle(Rect rect, bool value, TipSignal? tooltip = null, bool enabled = true, Texture2D onTex = null, Texture2D offTex = null, bool highlight = true)
        {
            SetEnabledColor(enabled);

            var result = value;
            if (enabled && Widgets.ButtonInvisible(rect))
            {
                result = !value;
                if (result) { SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera(); }
                else { SoundDefOf.Checkbox_TurnedOff.PlayOneShotOnCamera(); }
            }

            DrawTooltip(rect, tooltip, highlight);

            var image = result ? onTex ?? Widgets.CheckboxOnTex : offTex ?? Widgets.CheckboxOffTex;
            GUI.DrawTexture(rect, image);
            ResetColor();

            return result;
        }

        public static void DrawBar(Rect rect, float percentage, Color color)
        {
            Widgets.DrawBoxSolid(rect, Theme.BarBackgroundColor.Value);
            Widgets.DrawBoxSolid(rect.LeftPart(percentage), color);
        }

        public static void DrawTooltip(Rect rect, TipSignal? tooltip, bool highlight)
        {
            //if ((tooltip == null) || !Mouse.IsOver(rect) || (tooltip.Value.text.NullOrEmpty() && (tooltip.Value.textGetter == null))) { return; }
            if ((tooltip == null) || !Mouse.IsOver(rect)) { return; }

            if (highlight) { Widgets.DrawHighlight(rect); }
            TooltipHandler.TipRegion(rect, tooltip.Value);
        }

        public static Color HexToColor(string hex) => ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out var color) ? color : default(Color);
    }
}
