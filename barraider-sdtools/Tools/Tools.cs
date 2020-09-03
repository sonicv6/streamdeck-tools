﻿using BarRaider.SdTools.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace BarRaider.SdTools
{
    /// <summary>
    /// Set of common utilities used by various plugins
    /// Currently the class mostly focuses on image-related functions that will be passed to the StreamDeck key
    /// </summary>
    public static class Tools
    {
        private const string HEADER_PREFIX = "data:image/png;base64,";
        private const int CLASSIC_KEY_DEFAULT_HEIGHT = 72;
        private const int CLASSIC_KEY_DEFAULT_WIDTH = 72;
        private const int XL_KEY_DEFAULT_HEIGHT = 96;
        private const int XL_KEY_DEFAULT_WIDTH = 96;
        private const int GENERIC_KEY_IMAGE_SIZE = 144;

        #region Image Related

        /// <summary>
        /// Convert an image file to Base64 format. Set the addHeaderPrefix to true, if this is sent to the SendImageAsync function
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="addHeaderPrefix"></param>
        /// <returns></returns>
        public static string FileToBase64(string fileName, bool addHeaderPrefix)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            using (Image image = Image.FromFile(fileName))
            {
                return ImageToBase64(image, addHeaderPrefix);
            }
        }

        /// <summary>
        /// Convert a in-memory image object to Base64 format. Set the addHeaderPrefix to true, if this is sent to the SendImageAsync function
        /// </summary>
        /// <param name="image"></param>
        /// <param name="addHeaderPrefix"></param>
        /// <returns></returns>
        public static string ImageToBase64(Image image, bool addHeaderPrefix)
        {
            if (image == null)
            {
                return null;
            }

            using (MemoryStream m = new MemoryStream())
            {
                image.Save(m, ImageFormat.Png);
                byte[] imageBytes = m.ToArray();

                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                return addHeaderPrefix ? HEADER_PREFIX + base64String : base64String;
            }
        }

        /// <summary>
        /// Convert a base64 image string to an Image object
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns></returns>
        public static Image Base64StringToImage(string base64String)
        {
            try
            {
                if (string.IsNullOrEmpty(base64String))
                {
                    return null;
                }

                // Remove header
                if (base64String.Substring(0, HEADER_PREFIX.Length) == HEADER_PREFIX)
                {
                    base64String = base64String.Substring(HEADER_PREFIX.Length);
                }

                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (MemoryStream m = new MemoryStream(imageBytes))
                {
                    return Image.FromStream(m);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Base64StringToImage Exception: {ex}");
            }
            return null;
        }

        /// <summary>
        /// Gets the key default height in pixels.
        /// To get the StreamDeckType use Connection.DeviceInfo()
        /// </summary>
        /// <param name="streamDeckType"></param>
        /// <returns></returns>
        public static int GetKeyDefaultHeight(StreamDeckDeviceType streamDeckType)
        {
            switch (streamDeckType)
            {
                case StreamDeckDeviceType.StreamDeckClassic:
                case StreamDeckDeviceType.StreamDeckMini:
                case StreamDeckDeviceType.StreamDeckMobile:
                    return CLASSIC_KEY_DEFAULT_HEIGHT;
                case StreamDeckDeviceType.StreamDeckXL:
                    return XL_KEY_DEFAULT_HEIGHT;
                default:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"SDTools GetKeyDefaultHeight Error: Invalid StreamDeckDeviceType: {streamDeckType}");
                    break;
            }
            return 1;
        }

        /// <summary>
        /// Gets the key default width in pixels.
        /// To get the StreamDeckType use Connection.DeviceInfo()
        /// </summary>
        /// <param name="streamDeckType"></param>
        /// <returns></returns>
        public static int GetKeyDefaultWidth(StreamDeckDeviceType streamDeckType)
        {
            switch (streamDeckType)
            {
                case StreamDeckDeviceType.StreamDeckClassic:
                case StreamDeckDeviceType.StreamDeckMini:
                case StreamDeckDeviceType.StreamDeckMobile:
                    return CLASSIC_KEY_DEFAULT_WIDTH;
                case StreamDeckDeviceType.StreamDeckXL:
                    return XL_KEY_DEFAULT_WIDTH;
                default:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"SDTools GetKeyDefaultHeight Error: Invalid StreamDeckDeviceType: {streamDeckType}");
                    break;
            }
            return 1;
        }

        /// <summary>
        /// Generates an empty key bitmap with the default height and width.
        /// New: To get the StreamDeckType use Connection.DeviceInfo()
        /// </summary>
        /// <param name="streamDeckType"></param>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static Bitmap GenerateKeyImage(StreamDeckDeviceType streamDeckType, out Graphics graphics)
        {
            int height = GetKeyDefaultHeight(streamDeckType);
            int width = GetKeyDefaultWidth(streamDeckType);

            return GenerateKeyImage(height, width, out graphics);
        }

        /// <summary>
        /// Creates a key image that fits all Stream Decks
        /// </summary>
        /// <param name="graphics"></param>
        /// <returns></returns>
        public static Bitmap GenerateGenericKeyImage(out Graphics graphics)
        {
            return GenerateKeyImage(GENERIC_KEY_IMAGE_SIZE, GENERIC_KEY_IMAGE_SIZE, out graphics);
        }

        /// <summary>
        /// Creates a key image based on given height and width
        /// </summary>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <param name="graphics"></param>
        /// <returns></returns>
        private static Bitmap GenerateKeyImage(int height, int width, out Graphics graphics)
        {
            try
            {
                Bitmap bitmap = new Bitmap(width, height);
                var brush = new SolidBrush(Color.Black);

                graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                //Fill background black
                graphics.FillRectangle(brush, 0, 0, width, height);
                return bitmap;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"SDTools GenerateKeyImage exception: {ex} Height: {height} Width: {width}");
            }
            graphics = null;
            return null;
        }

        /// <summary>
        /// Deprecated! Use AddTextPath on the Graphics extension method instead.
        /// Adds a text path to an existing Graphics object. Uses TitleParser to emulate the Text settings in the Property Inspector
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="titleParameters"></param>
        /// <param name="imageHeight"></param>
        /// <param name="imageWidth"></param>
        /// <param name="text"></param>
        /// <param name="pixelsAlignment"></param>
        [Obsolete("Use graphics.AddTextPath() extension method instead")]
        public static void AddTextPathToGraphics(Graphics graphics, TitleParameters titleParameters, int imageHeight, int imageWidth, string text, int pixelsAlignment = 15)
        {
            if (graphics != null)
            {
                graphics.AddTextPath(titleParameters, imageHeight, imageWidth, text, pixelsAlignment);
            }
        }

        #endregion

        #region Filename Related

        /// <summary>
        /// Extracts the actual filename from a file payload received from the Property Inspector
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static string FilenameFromPayload(Newtonsoft.Json.Linq.JToken payload)
        {
            return FilenameFromString((string)payload);
        }

        private static string FilenameFromString(string filenameWithFakepath)
        {
            if (string.IsNullOrEmpty(filenameWithFakepath))
            {
                return null;
            }

            return Uri.UnescapeDataString(filenameWithFakepath.Replace("C:\\fakepath\\", ""));
        }

        #endregion

        #region String Related

        /// <summary>
        /// Converts a long to a human-readable string. Example: 54,265 => 54.27k
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static string FormatNumber(long num)
        {
            if (num >= 100000000)
            {
                return (num / 1000000D).ToString("0.#M");
            }
            if (num >= 1000000)
            {
                return (num / 1000000D).ToString("0.##M");
            }
            if (num >= 100000)
            {
                return (num / 1000D).ToString("0.#k");
            }
            if (num >= 10000)
            {
                return (num / 1000D).ToString("0.##k");
            }

            return num.ToString("#,0");
        }

        /// <summary>
        /// Adds line breaks (\n) to the text to make sure it fits the key when using SetTitleAsync()
        /// </summary>
        /// <param name="str"></param>
        /// <param name="titleParameters"></param>
        /// <param name="leftPaddingPixels"></param>
        /// <param name="rightPaddingPixels"></param>
        /// <param name="imageWidthPixels"></param>
        /// <returns></returns>
        public static string SplitStringToFit(string str, TitleParameters titleParameters, int leftPaddingPixels = 3, int rightPaddingPixels = 3, int imageWidthPixels = 72)
        {
            try
            {
                if (titleParameters == null)
                {
                    return str;
                }

                int padding = leftPaddingPixels + rightPaddingPixels;
                Font font = new Font(titleParameters.FontFamily, (float)titleParameters.FontSizeInPoints, titleParameters.FontStyle, GraphicsUnit.Pixel);
                StringBuilder finalString = new StringBuilder();
                StringBuilder currentLine = new StringBuilder();
                SizeF currentLineSize;

                using (Bitmap img = new Bitmap(imageWidthPixels, imageWidthPixels))
                {
                    using (Graphics graphics = Graphics.FromImage(img))
                    {
                        for (int idx = 0; idx < str.Length; idx++)
                        {
                            currentLine.Append(str[idx]);
                            currentLineSize = graphics.MeasureString(currentLine.ToString(), font);
                            if (currentLineSize.Width <= img.Width - padding)
                            {
                                finalString.Append(str[idx]);
                            }
                            else // Overflow
                            {
                                finalString.Append("\n" + str[idx]);
                                currentLine = new StringBuilder(str[idx].ToString());
                            }
                        }
                    }
                }

                return finalString.ToString();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"SplitStringToFit Exception: {ex}");
                return str;
            }
        }

        #endregion

        #region SHA512

        /// <summary>
        /// Returns SHA512 Hash from an image object
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string ImageToSHA512(Image image)
        {
            if (image == null)
            {
                return null;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    return BytesToSHA512(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"ImageToSHA512 Exception: {ex}");
            }
            return null;
        }

        /// <summary>
        /// Returns SHA512 Hash from a string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string StringToSHA512(string str)
        {
            if (str == null)
            {
                return null;
            }
            return BytesToSHA512(System.Text.Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Returns SHA512 Hash from a byte stream
        /// </summary>
        /// <param name="byteStream"></param>
        /// <returns></returns>
        public static string BytesToSHA512(byte[] byteStream)
        {
            try
            {
                SHA512CryptoServiceProvider sha512 = new SHA512CryptoServiceProvider();
                byte[] hash = sha512.ComputeHash(byteStream);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"BytesToSHA512 Exception: {ex}");
            }
            return null;
        }

        #endregion

        #region MD5

        /// <summary>
        /// Returns MD5 Hash from an image object
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        [Obsolete("Use ImageToSHA512 instead. MD5 is not FIPS compliant")]
        public static string ImageToMD5(Image image)
        {
            Logger.Instance.LogMessage(TracingLevel.WARN, $"ImageToMD5 is obsolete and will soon be deprecated");
            if (image == null)
            {
                return null;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    image.Save(ms, ImageFormat.Png);
                    return BytesToMD5(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"ImageToMD5 Exception: {ex}");
            }
            return null;
        }

        /// <summary>
        /// Returns MD5 Hash from a string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        [Obsolete("Use StringToSHA512 instead. MD5 is not FIPS compliant")]
        public static string StringToMD5(string str)
        {
            Logger.Instance.LogMessage(TracingLevel.WARN, $"StringToMD5 is obsolete and will soon be deprecated");
            if (str == null)
            {
                return null;
            }
            return BytesToMD5(System.Text.Encoding.UTF8.GetBytes(str));
        }

        /// <summary>
        /// Returns MD5 Hash from a byte stream
        /// </summary>
        /// <param name="byteStream"></param>
        /// <returns></returns>
        [Obsolete("Use BytesToSHA512 instead. MD5 is not FIPS compliant")]
        public static string BytesToMD5(byte[] byteStream)
        {
            Logger.Instance.LogMessage(TracingLevel.WARN, $"BytesToMD5 is obsolete and will soon be deprecated");
            try
            {
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
                byte[] hash = md5.ComputeHash(byteStream);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"BytesToMD5 Exception: {ex}");
            }
            return null;
        }

        #endregion

        #region JObject Related

        /// <summary>
        /// Iterates through the fromJObject, finds the property that matches in the toSettings object, and sets the value from the fromJObject object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="toSettings"></param>
        /// <param name="fromJObject"></param>
        /// <returns>Number of properties updated</returns>
        public static int AutoPopulateSettings<T>(T toSettings, JObject fromJObject)
        {
            Dictionary<string, PropertyInfo> dicProperties = MatchPropertiesWithJsonProperty(toSettings);
            int totalPopulated = 0;

            if (fromJObject != null)
            {
                foreach (var prop in fromJObject)
                {
                    if (dicProperties.ContainsKey(prop.Key))
                    {
                        PropertyInfo info = dicProperties[prop.Key];

                        // Special handling for FilenameProperty
                        if (info.GetCustomAttributes(typeof(FilenamePropertyAttribute), true).Length > 0)
                        {
                            string value = FilenameFromString((string)prop.Value);
                            info.SetValue(toSettings, value);
                        }
                        else
                        {
                            info.SetValue(toSettings, Convert.ChangeType(prop.Value, info.PropertyType));
                        }
                        totalPopulated++;
                    }
                }
            }
            return totalPopulated;
        }

        private static Dictionary<string, PropertyInfo> MatchPropertiesWithJsonProperty<T>(T obj)
        {
            Dictionary<string, PropertyInfo> dicProperties = new Dictionary<string, PropertyInfo>();
            if (obj != null)
            {
                PropertyInfo[] props = typeof(T).GetProperties();
                foreach (PropertyInfo prop in props)
                {
                    object[] attributes = prop.GetCustomAttributes(true);
                    foreach (object attr in attributes)
                    {
                        if (attr is JsonPropertyAttribute jprop)
                        {
                            dicProperties.Add(jprop.PropertyName, prop);
                            break;
                        }
                    }
                }
            }

            return dicProperties;
        }

        #endregion

        #region Plugin Helper Classes

        /// <summary>
        /// Uses the PluginActionId attribute on the various classes derived from PluginBase to find all the actions supported in this assembly
        /// </summary>
        /// <returns></returns>
        public static PluginActionId[] AutoLoadPluginActions()
        {
            List<PluginActionId> actions = new List<PluginActionId>();

            var pluginTypes = Assembly.GetEntryAssembly().GetTypes().Where(typ => typ.IsClass && typ.GetCustomAttributes(typeof(PluginActionIdAttribute), true).Length > 0).ToList();
            pluginTypes.ForEach(typ =>
            {
                if (typ.GetCustomAttributes(typeof(PluginActionIdAttribute), true).First() is PluginActionIdAttribute attr)
                {
                    actions.Add(new PluginActionId(attr.ActionId, typ));
                }
            });

            return actions.ToArray();
        }

        #endregion
    }
}