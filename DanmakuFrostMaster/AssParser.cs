﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Windows.UI;

namespace Atelier39
{
    public static class AssParser
    {
        public static List<DanmakuItem> GetDanmakuList(string assStr)
        {
            try
            {
                List<DanmakuItem> list = new List<DanmakuItem>();
                Dictionary<string, AssStyle> styleDict = new Dictionary<string, AssStyle>();

                using (StringReader stringReader = new StringReader(assStr))
                {
                    int videoWidth = 0;
                    int videoHeight = 0;
                    int dialogueFormatSegmentCount = 10;

                    string line = stringReader.ReadLine();
                    while (line != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            if (line.StartsWith("PlayResX:"))
                            {
                                int.TryParse(line.Substring(9).Trim(), out videoWidth);
                            }
                            else if (line.StartsWith("PlayResY:"))
                            {
                                int.TryParse(line.Substring(9).Trim(), out videoHeight);
                            }
                            else if (line.StartsWith("Style:"))
                            {
                                try
                                {
                                    line = line.Substring(6).Trim();
                                    string[] splitArray = line.Split(',');
                                    if (splitArray.Length >= 22)
                                    {
                                        string styleName = splitArray[0];
                                        if (!styleDict.ContainsKey(styleName))
                                        {
                                            styleDict.Add(styleName, new AssStyle());
                                        }
                                        AssStyle style = styleDict[styleName];

                                        style.FontFamilyName = splitArray[1];
                                        float.TryParse(splitArray[2], out style.FontSize);

                                        Match textColorMatch = Regex.Match(splitArray[3], "[A-Fa-f0-9]{8}");
                                        if (textColorMatch.Success)
                                        {
                                            string colorStr = textColorMatch.Value;
                                            if (TryParseHexToByte(colorStr.Substring(0, 2), out byte a)
                                                && TryParseHexToByte(colorStr.Substring(6, 2), out byte r)
                                                && TryParseHexToByte(colorStr.Substring(4, 2), out byte g)
                                                && TryParseHexToByte(colorStr.Substring(2, 2), out byte b))
                                            {
                                                style.Alpha = (byte)(byte.MaxValue - a);
                                                style.TextColor = Color.FromArgb(byte.MaxValue, r, g, b);
                                            }
                                        }
                                        Match outlineColorMatch = Regex.Match(splitArray[5], "[A-Fa-f0-9]{8}");
                                        if (outlineColorMatch.Success)
                                        {
                                            string colorStr = outlineColorMatch.Value;
                                            if (TryParseHexToByte(colorStr.Substring(0, 2), out byte a)
                                                && TryParseHexToByte(colorStr.Substring(6, 2), out byte r)
                                                && TryParseHexToByte(colorStr.Substring(4, 2), out byte g)
                                                && TryParseHexToByte(colorStr.Substring(2, 2), out byte b))
                                            {
                                                style.OutlineColor = Color.FromArgb(byte.MaxValue, r, g, b);
                                            }
                                        }

                                        style.IsBold = splitArray[7] == "-1";
                                        style.HasOutline = splitArray[15] == "1";
                                        float.TryParse(splitArray[16], out style.OutlineSize);

                                        if (int.TryParse(splitArray[18], out int positionMode))
                                        {
                                            style.AlignmentMode = (DanmakuAlignmentMode)positionMode;
                                        }
                                        if (int.TryParse(splitArray[19], out int marginLeft))
                                        {
                                            style.MarginLeft = marginLeft;
                                        }
                                        if (int.TryParse(splitArray[20], out int marginRight))
                                        {
                                            style.MarginRight = marginRight;
                                        }
                                        if (int.TryParse(splitArray[21], out int marginBottom))
                                        {
                                            style.MarginBottom = marginBottom;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Failed to parse ASS style ({line}): {ex.Message}");
                                }
                            }
                            else if (line.StartsWith("[Events]"))
                            {
                                line = stringReader.ReadLine();
                                if (line.StartsWith("Format:"))
                                {
                                    dialogueFormatSegmentCount = line.Split(',').Length;
                                }
                            }
                            else if (line.StartsWith("Dialogue:"))
                            {
                                try
                                {
                                    line = line.Substring(8).Trim();
                                    string[] splitArray = line.Split(',');
                                    string subtitleText = splitArray.Last().Trim();
                                    if (splitArray.Length > dialogueFormatSegmentCount)
                                    {
                                        subtitleText = splitArray[dialogueFormatSegmentCount - 1];
                                        for (int i = dialogueFormatSegmentCount; i < splitArray.Length; i++)
                                        {
                                            subtitleText = string.Concat(subtitleText, ",", splitArray[i]);
                                        }
                                    }

                                    if (!string.IsNullOrWhiteSpace(subtitleText))
                                    {
                                        DanmakuItem danmakuItem = new DanmakuItem();
                                        danmakuItem.AllowDensityControl = false;
                                        danmakuItem.Mode = DanmakuMode.Advanced;

                                        if (TimeSpan.TryParse(splitArray[1], out TimeSpan startTime) && TimeSpan.TryParse(splitArray[2], out TimeSpan endTime) && endTime.TotalMilliseconds > 0)
                                        {
                                            danmakuItem.StartMs = (uint)Math.Max(startTime.TotalMilliseconds, 0);
                                            danmakuItem.DurationMs = (uint)endTime.TotalMilliseconds - danmakuItem.StartMs;

                                            string styleName = splitArray[3];
                                            if (styleName.StartsWith("*"))
                                            {
                                                styleName = styleName.Substring(1);
                                            }
                                            AssStyle style = styleDict.ContainsKey(styleName) ? styleDict[styleName] : new AssStyle();
                                            danmakuItem.StartAlpha = style.Alpha;
                                            danmakuItem.EndAlpha = style.Alpha;
                                            danmakuItem.IsBold = style.IsBold;
                                            danmakuItem.HasOutline = style.HasOutline;
                                            danmakuItem.BaseFontSize = style.FontSize;
                                            danmakuItem.OutlineSize = style.OutlineSize;
                                            danmakuItem.FontFamilyName = style.FontFamilyName;
                                            danmakuItem.TextColor = style.TextColor;
                                            danmakuItem.OutlineColor = style.OutlineColor;
                                            danmakuItem.MarginLeft = style.MarginLeft;
                                            danmakuItem.MarginRight = style.MarginRight;
                                            danmakuItem.MarginBottom = style.MarginBottom;
                                            danmakuItem.AlignmentMode = style.AlignmentMode;

                                            if (int.TryParse(splitArray[5], out int marginLeft) && marginLeft != 0)
                                            {
                                                danmakuItem.MarginLeft = marginLeft;
                                            }
                                            if (int.TryParse(splitArray[6], out int marginRight) && marginRight != 0)
                                            {
                                                danmakuItem.MarginRight = marginRight;
                                            }
                                            if (int.TryParse(splitArray[7], out int marginBottom) && marginBottom != 0)
                                            {
                                                danmakuItem.MarginBottom = marginBottom;
                                            }

                                            if (subtitleText.StartsWith("{") && subtitleText.Contains("}") && !subtitleText.EndsWith("}"))
                                            {
                                                Match fnMatch = Regex.Match(subtitleText, @"\\fn(?<font>.+?)[\\\}]");
                                                if (fnMatch.Success)
                                                {
                                                    danmakuItem.FontFamilyName = fnMatch.Groups["font"].Value;
                                                }

                                                Match fsMatch = Regex.Match(subtitleText, @"\\fs(?<fs>\d+)");
                                                if (fsMatch.Success && int.TryParse(fsMatch.Groups["fs"].Value, out int fs))
                                                {
                                                    danmakuItem.BaseFontSize = fs;
                                                }

                                                Match bordMatch = Regex.Match(subtitleText, @"\\bord(?<bord>\d+)");
                                                if (bordMatch.Success && int.TryParse(bordMatch.Groups["bord"].Value, out int bord))
                                                {
                                                    danmakuItem.OutlineSize = bord;
                                                    danmakuItem.HasOutline = bord > 0;
                                                }

                                                MatchCollection colorMatchCollection = Regex.Matches(subtitleText, @"\\(?<colorType>\d)c.*?(?<color>[A-Fa-f0-9]{6,8})");
                                                foreach (Match colorMatch in colorMatchCollection)
                                                {
                                                    int colorType = int.Parse(colorMatch.Groups["colorType"].Value);
                                                    if (colorType == 1 || colorType == 3)
                                                    {
                                                        string colorStr = colorMatch.Groups["color"].Value;
                                                        if (colorStr.Length > 6)
                                                        {
                                                            colorStr = colorStr.Substring(colorStr.Length - 6, 6);
                                                        }
                                                        if (TryParseHexToByte(colorStr.Substring(4, 2), out byte r)
                                                            && TryParseHexToByte(colorStr.Substring(2, 2), out byte g)
                                                            && TryParseHexToByte(colorStr.Substring(0, 2), out byte b))
                                                        {
                                                            if (colorType == 3)
                                                            {
                                                                danmakuItem.OutlineColor = Color.FromArgb(byte.MaxValue, r, g, b);
                                                            }
                                                            else
                                                            {
                                                                danmakuItem.TextColor = Color.FromArgb(byte.MaxValue, r, g, b);
                                                            }
                                                        }
                                                    }
                                                }

                                                Match fadMatch = Regex.Match(subtitleText, @"\\fad\((?<fadeInMs>\d+),(?<fadeOutMs>\d+)\)");
                                                if (fadMatch.Success)
                                                {
                                                    uint fadeInMs = uint.Parse(fadMatch.Groups["fadeInMs"].Value);
                                                    if (fadeInMs > 0)
                                                    {
                                                        danmakuItem.StartAlpha = 0;
                                                        danmakuItem.AlphaDurationMs = fadeInMs;
                                                    }
                                                    else
                                                    {
                                                        uint fadeOutMs = uint.Parse(fadMatch.Groups["fadeOutMs"].Value);
                                                        if (fadeOutMs > 0)
                                                        {
                                                            danmakuItem.EndAlpha = 0;
                                                            danmakuItem.AlphaDurationMs = fadeOutMs;
                                                        }
                                                    }
                                                }

                                                Match moveMatch = Regex.Match(subtitleText, @"\\move\((?<move>[\d\.,]+?)\)");
                                                if (moveMatch.Success)
                                                {
                                                    danmakuItem.AlignmentMode = DanmakuAlignmentMode.Default;

                                                    string moveStr = moveMatch.Groups["move"].Value;
                                                    string[] moveSplitArray = moveStr.Split(',');
                                                    if (moveSplitArray.Length >= 4)
                                                    {
                                                        float startX = float.Parse(moveSplitArray[0]);
                                                        float startY = float.Parse(moveSplitArray[1]);
                                                        float endX = float.Parse(moveSplitArray[2]);
                                                        float endY = float.Parse(moveSplitArray[3]);
                                                        if (videoWidth > 0 && videoHeight > 0)
                                                        {
                                                            startX = Math.Min(startX / videoWidth, 0.99f);
                                                            startY = Math.Min(startY / videoHeight, 0.99f);
                                                            endX = Math.Min(endX / videoWidth, 0.99f);
                                                            endY = Math.Min(endY / videoHeight, 0.99f);
                                                        }
                                                        danmakuItem.StartX = startX;
                                                        danmakuItem.EndX = endX;
                                                        danmakuItem.StartY = startY;
                                                        danmakuItem.EndY = endY;

                                                        if (moveSplitArray.Length >= 6)
                                                        {
                                                            if (ulong.TryParse(moveSplitArray[4], out ulong delayMs) && ulong.TryParse(moveSplitArray[5], out ulong durationMs))
                                                            {
                                                                danmakuItem.TranslationDelayMs = delayMs;
                                                                danmakuItem.TranslationDurationMs = durationMs;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Match posMatch = Regex.Match(subtitleText, @"\\pos\((?<x>[\d\.]+),(?<y>[\d\.]+)\)");
                                                    if (posMatch.Success)
                                                    {
                                                        danmakuItem.AlignmentMode = DanmakuAlignmentMode.Default;

                                                        float x = float.Parse(posMatch.Groups["x"].Value);
                                                        float y = float.Parse(posMatch.Groups["y"].Value);
                                                        if (videoWidth > 0 && videoHeight > 0)
                                                        {
                                                            x = Math.Min(x / videoWidth, 0.99f);
                                                            y = Math.Min(y / videoHeight, 0.99f);
                                                        }
                                                        danmakuItem.StartX = x;
                                                        danmakuItem.EndX = x;
                                                        danmakuItem.StartY = y;
                                                        danmakuItem.EndY = y;

                                                        Match anMatch = Regex.Match(subtitleText, @"\\an(?<an>\d+)");
                                                        if (anMatch.Success)
                                                        {
                                                            int an = int.Parse(anMatch.Groups["an"].Value);
                                                            danmakuItem.AnchorMode = (DanmakuAlignmentMode)an;
                                                        }
                                                        else
                                                        {
                                                            danmakuItem.AnchorMode = style.AlignmentMode;
                                                        }
                                                    }
                                                }

                                                Match fryMatch = Regex.Match(subtitleText, @"\\fry(?<fry>[0-9\.]+)");
                                                if (fryMatch.Success && float.TryParse(fryMatch.Groups["fry"].Value, out float fry))
                                                {
                                                    danmakuItem.RotateY = fry;
                                                }

                                                Match frzMatch = Regex.Match(subtitleText, @"\\frz(?<frz>[0-9\.]+)");
                                                if (frzMatch.Success && float.TryParse(frzMatch.Groups["frz"].Value, out float frz))
                                                {
                                                    danmakuItem.RotateZ = -frz;
                                                }

                                                subtitleText = Regex.Replace(subtitleText, @"\{.*?\}", string.Empty);
                                                if (Regex.IsMatch(subtitleText, @"^m \d+"))
                                                {
                                                    // Skip drawing command
                                                    continue;
                                                }
                                            }
                                            danmakuItem.Text = subtitleText.Replace(@"\n", "\n").Replace(@"\N", "\n");

                                            danmakuItem.BaseFontSize = Math.Max(danmakuItem.BaseFontSize * 0.75f, 2);
                                            danmakuItem.OutlineSize *= 2;
                                            if (danmakuItem.FontFamilyName.EndsWith(" Bold"))
                                            {
                                                danmakuItem.FontFamilyName = danmakuItem.FontFamilyName.Substring(0, danmakuItem.FontFamilyName.Length - 5);
                                            }
                                            if (danmakuItem.FontFamilyName.StartsWith("@"))
                                            {
                                                danmakuItem.FontFamilyName = danmakuItem.FontFamilyName.Substring(1);
                                            }

                                            if (danmakuItem.AlignmentMode == DanmakuAlignmentMode.LowerCenter && danmakuItem.AlphaDurationMs == 0)
                                            {
                                                danmakuItem.Mode = DanmakuMode.Bottom;
                                            }

                                            list.Add(danmakuItem);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Failed to parse ASS dialogue ({line}): {ex.Message}");
                                }
                            }
                        }
                        line = stringReader.ReadLine();
                    }
                }

                list.Sort((a, b) => (int)a.StartMs - (int)b.StartMs);
                // Avoid startMs/endMs overlapping so they are not rendered to stacking lines as much as possible
                for (int i = 0; i < list.Count - 1; i++)
                {
                    if (list[i].Mode == DanmakuMode.Bottom
                        && list[i + 1].Mode == DanmakuMode.Bottom
                        && list[i].DurationMs > 100
                        && list[i + 1].StartMs - list[i].StartMs - list[i].DurationMs < 60)
                    {
                        list[i].DurationMs = list[i + 1].StartMs - list[i].StartMs - 60;
                    }
                }

                return list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to parse ASS: {ex.Message}");
                return null;
            }
        }

        private static bool TryParseHexToByte(string str, out byte value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(str) || str.Length > 2)
            {
                return false;
            }

            str = str.ToUpper();
            char c0 = str[0];
            if (!((c0 >= '0' && c0 <= '9') || (c0 >= 'A' && c0 <= 'F')))
            {
                return false;
            }

            if (str.Length == 1)
            {
                if (c0 >= 'A' && c0 <= 'F')
                {
                    value = (byte)(c0 - 'A' + 10);
                }
                else
                {
                    value = (byte)(c0 - '0');
                }
            }
            else
            {
                if (c0 >= 'A' && c0 <= 'F')
                {
                    value = (byte)((c0 - 'A' + 10) << 4);
                }
                else
                {
                    value = (byte)((c0 - '0') << 4);
                }

                char c1 = str[1];
                if (!((c1 >= '0' && c1 <= '9') || (c1 >= 'A' && c1 <= 'F')))
                {
                    return false;
                }

                if (c1 >= 'A' && c1 <= 'F')
                {
                    value += (byte)(c1 - 'A' + 10);
                }
                else
                {
                    value += (byte)(c1 - '0');
                }
            }

            return true;
        }

        private class AssStyle
        {
            public int MarginLeft = 0;
            public int MarginRight = 0;
            public int MarginBottom = 20;
            public byte Alpha = byte.MaxValue;
            public bool IsBold = true;
            public bool HasOutline = true;
            public float FontSize = (uint)DanmakuItem.DefaultBaseFontSize;
            public float OutlineSize = 2f;
            public string FontFamilyName = null;
            public Color TextColor = Colors.White;
            public Color OutlineColor = Colors.Black;
            public DanmakuAlignmentMode AlignmentMode = DanmakuAlignmentMode.LowerCenter;
        }
    }
}
