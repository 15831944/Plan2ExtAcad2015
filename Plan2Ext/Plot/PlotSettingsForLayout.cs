﻿#if BRX_APP
using _AcAp = Bricscad.ApplicationServices;
using _AcCm = Teigha.Colors;
using _AcDb = Teigha.DatabaseServices;
using _AcEd = Bricscad.EditorInput;
using _AcGe = Teigha.Geometry;
using _AcGi = Teigha.GraphicsInterface;
using _AcGs = Teigha.GraphicsSystem;
using _AcPl = Bricscad.PlottingServices;
using _AcBrx = Bricscad.Runtime;
using _AcTrx = Teigha.Runtime;
using _AcWnd = Bricscad.Windows;
using _AcIntCom = BricscadDb;
using _AcInt = BricscadApp;
#elif ARX_APP
using _AcAp = Autodesk.AutoCAD.ApplicationServices;
using _AcCm = Autodesk.AutoCAD.Colors;
using _AcDb = Autodesk.AutoCAD.DatabaseServices;
using _AcEd = Autodesk.AutoCAD.EditorInput;
using _AcGe = Autodesk.AutoCAD.Geometry;
using _AcGi = Autodesk.AutoCAD.GraphicsInterface;
using _AcGs = Autodesk.AutoCAD.GraphicsSystem;
using _AcPl = Autodesk.AutoCAD.PlottingServices;
using _AcBrx = Autodesk.AutoCAD.Runtime;
using _AcTrx = Autodesk.AutoCAD.Runtime;
using _AcWnd = Autodesk.AutoCAD.Windows;
using _AcIntCom = Autodesk.AutoCAD.Interop.Common;
using _AcInt = Autodesk.AutoCAD.Interop;
#endif

using System;
using System.Globalization;
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
// ReSharper disable StringLiteralTypo


namespace Plan2Ext.Plot
{
    public static class Extensions
    {
        #region log4net Initialization
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(Convert.ToString((typeof(Extensions))));
        #endregion

        private static bool ContainsIgnoreUc(this System.Collections.Specialized.StringCollection coll, ref string key)
        {
            foreach (var k in coll)
            {
                if (string.Compare(k, key, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    key = k;
                    return true;
                }
            }
            return false;
        }

        public static void SetPlotSettings(this _AcDb.Layout lay, string device, string mediaName, string styleSheet, double? scaleNumerator, double? scaleDenominator, short? rotation)
        {
            using (var ps = new _AcDb.PlotSettings(lay.ModelType))
            {
                ps.CopyFrom(lay);

                var psv = _AcDb.PlotSettingsValidator.Current;

                // Set the device
                if (!string.IsNullOrEmpty(device))
                {
                    var devs = psv.GetPlotDeviceList();

                    if (devs.ContainsIgnoreUc(ref device))
                    {
                        Log.InfoFormat(CultureInfo.CurrentCulture, "Setze Device '{0}' für Layout '{1}'.", device, lay.LayoutName);
                        psv.SetPlotConfigurationName(ps, device, null);
                        psv.RefreshLists(ps);
                    }
                    else
                    {
	                    if (device.Equals("Kein", StringComparison.CurrentCultureIgnoreCase))
	                    {
							// english variante
							device = "None";
							if (devs.ContainsIgnoreUc(ref device))
							{
								Log.InfoFormat(CultureInfo.CurrentCulture, "Setze Device '{0}' für Layout '{1}'.", device, lay.LayoutName);
								psv.SetPlotConfigurationName(ps, device, null);
								psv.RefreshLists(ps);
							}
							else
							{ Log.WarnFormat(CultureInfo.CurrentCulture, "Device '{0}' existiert nicht!", device); }
						}
						else
	                    { Log.WarnFormat(CultureInfo.CurrentCulture, "Device '{0}' existiert nicht!", device);}
                    }
                }

                // Set the media name/size

                if (!string.IsNullOrEmpty(mediaName))
                {

                    var mns = psv.GetCanonicalMediaNameList(ps);
                    if (mns.ContainsIgnoreUc(ref mediaName))
                    {
                        Log.InfoFormat(CultureInfo.CurrentCulture, "Setze PageSize '{0}' für Layout '{1}'.", mediaName, lay.LayoutName);
                        psv.SetCanonicalMediaName(ps, mediaName);
                    }
                    else
                    {
                        string canonicalMediaName = LocaleToCanonicalMediaName(psv, ps, mediaName);
                        if (!string.IsNullOrEmpty(canonicalMediaName))
                        {
                            Log.InfoFormat(CultureInfo.CurrentCulture, "Setze PageSize '{0}' für Layout '{1}'.", canonicalMediaName, lay.LayoutName);
                            psv.SetCanonicalMediaName(ps, canonicalMediaName);
                        }
                        else
                        {
                            Log.WarnFormat(CultureInfo.CurrentCulture, "Size '{0}' existiert nicht!", mediaName);
                        }
                    }
                }

                // Set the pen settings
                var ssl = psv.GetPlotStyleSheetList();
                if (string.IsNullOrEmpty(styleSheet) || ssl.ContainsIgnoreUc(ref styleSheet))
                {
                    Log.InfoFormat(CultureInfo.CurrentCulture, "Setze StyleSheet '{0}' für Layout '{1}'.", styleSheet, lay.LayoutName);
                    psv.SetCurrentStyleSheet(ps, styleSheet);
                }
                else
                {
                    Log.WarnFormat(CultureInfo.CurrentCulture, "Stylesheet '{0}' existiert nicht!", styleSheet);
                }

                // Copy the PlotSettings data back to the Layout
                if (scaleNumerator.HasValue && scaleDenominator.HasValue)
                {
                    Log.InfoFormat(CultureInfo.CurrentCulture, "Setze Scale '{0}:{2}' für Layout '{1}'.", scaleNumerator.Value.ToString(CultureInfo.InvariantCulture), lay.LayoutName, scaleDenominator.Value.ToString(CultureInfo.InvariantCulture));
                    _AcDb.CustomScale cs = new _AcDb.CustomScale(scaleNumerator.Value, scaleDenominator.Value);

                    if (ps.PlotPaperUnits != _AcDb.PlotPaperUnit.Millimeters)
                    {
                        psv.SetPlotPaperUnits(ps, _AcDb.PlotPaperUnit.Millimeters);
                    }
                    psv.SetCustomPrintScale(ps, cs);
                }

                if (rotation.HasValue && (rotation.Value == 0 || rotation.Value == 90 || rotation.Value == 180 || rotation.Value == 270))
                {
                    _AcDb.PlotRotation pRot = _AcDb.PlotRotation.Degrees000;
                    switch (rotation.Value)
                    {
                        case 90:
                            pRot = _AcDb.PlotRotation.Degrees090;
                            break;
                        case 180:
                            pRot = _AcDb.PlotRotation.Degrees180;
                            break;
                        case 270:
                            pRot = _AcDb.PlotRotation.Degrees270;
                            break;
                    }
                    Log.InfoFormat(CultureInfo.CurrentCulture, "Setze Rotation '{0}' für Layout '{1}'.", pRot.ToString(), lay.LayoutName);
                    psv.SetPlotRotation(ps, pRot);
                }

                // plottype hardcoded auf fenster
                try
                {
                    if (ps.PlotType != _AcDb.PlotType.Window)
                    {
                        if (ps.PlotWindowArea.ToString() == "((0,0),(0,0))")
                        {
                            _AcDb.Extents2d e2D = new _AcDb.Extents2d(0.0, 0.0, 10.0, 10.0);
                            psv.SetPlotWindowArea(ps, e2D);
                        }
                        psv.SetPlotType(ps, _AcDb.PlotType.Window);
                    }

                }
                catch (Exception ex)
                {
                    Log.ErrorFormat(CultureInfo.CurrentCulture, "Fehler beim Setzen von PlotType auf FENSTER! {0}", ex.Message);
                }

                var upgraded = false;
                if (!lay.IsWriteEnabled)
                {
                    lay.UpgradeOpen();
                    upgraded = true;
                }

                lay.CopyFrom(ps);

                if (upgraded)
                {
                    lay.DowngradeOpen();
                }
            }
        }

        // ReSharper disable once UnusedMember.Global
        public static void SetPlotSettingsStyleSheet(this _AcDb.Layout lay, string styleSheet)
        {
            using (var ps = new _AcDb.PlotSettings(lay.ModelType))
            {
                ps.CopyFrom(lay);

                var psv = _AcDb.PlotSettingsValidator.Current;

                // Set the pen settings
                var ssl = psv.GetPlotStyleSheetList();
                if (string.IsNullOrEmpty(styleSheet) || ssl.ContainsIgnoreUc(ref styleSheet))
                {
                    Log.InfoFormat(CultureInfo.CurrentCulture, "Setze StyleSheet '{0}' für Layout '{1}'.", styleSheet, lay.LayoutName);
                    psv.SetCurrentStyleSheet(ps, styleSheet);
                }
                else
                {
                    Log.WarnFormat(CultureInfo.CurrentCulture, "Stylesheet '{0}' existiert nicht!", styleSheet);
                }

                var upgraded = false;
                if (!lay.IsWriteEnabled)
                {
                    lay.UpgradeOpen();
                    upgraded = true;
                }

                lay.CopyFrom(ps);

                if (upgraded)
                {
                    lay.DowngradeOpen();
                }
            }
        }

        // ReSharper disable once UnusedMember.Global
        public static void SetPlotSettingsDevice(this _AcDb.Layout lay, string device)
        {
            using (var ps = new _AcDb.PlotSettings(lay.ModelType))
            {
                ps.CopyFrom(lay);

                var psv = _AcDb.PlotSettingsValidator.Current;

                // Set the device
                if (!string.IsNullOrEmpty(device))
                {
                    var deviceList = psv.GetPlotDeviceList();

                    if (deviceList.ContainsIgnoreUc(ref device))
                    {
                        Log.InfoFormat(CultureInfo.CurrentCulture, "Setze Device '{0}' für Layout '{1}'.", device, lay.LayoutName);
                        psv.SetPlotConfigurationName(ps, device, null);
                        psv.RefreshLists(ps);
                    }
                    else
                    {
                        Log.WarnFormat(CultureInfo.CurrentCulture, "Device '{0}' existiert nicht!", device);
                    }
                }


                var upgraded = false;
                if (!lay.IsWriteEnabled)
                {
                    lay.UpgradeOpen();
                    upgraded = true;
                }

                lay.CopyFrom(ps);

                if (upgraded)
                {
                    lay.DowngradeOpen();
                }
            }
        }

        
        private static string LocaleToCanonicalMediaName(_AcDb.PlotSettingsValidator psv, _AcDb.PlotSettings ps, string mn)
        {
            int cnt = 0;

            foreach (string mediaName in psv.GetCanonicalMediaNameList(ps))
            {
                string localeMediaName = psv.GetLocaleMediaName(ps, cnt);
                if (string.Compare(mn, localeMediaName, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return mediaName;
                }

                cnt = cnt + 1;
            }

            return null;
        }

    }

    // ReSharper disable once UnusedMember.Global
    public class PlotSettingsForLayout
    {
        #region log4net Initialization
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(Convert.ToString((typeof(PlotSettingsForLayout))));
        #endregion

        [_AcTrx.LispFunction("Plan2OpenPlotSettings")]
        // ReSharper disable once UnusedMember.Global
        // ReSharper disable once UnusedParameter.Global
        public static void OpenPlotSettings(_AcDb.ResultBuffer rb)
        {
            try
            {
#if BRX_APP
	            _AcAp.Application.DocumentManager.MdiActiveDocument.SendStringToExecute("_CPAGESETUP ", true, false, false);
#else
				_AcAp.Application.DocumentManager.MdiActiveDocument.SendStringToExecute("Plot ", true, false, false);
#endif
			}
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
            }
        }

        /// <summary>
        /// Sets plot settings, if applicable
        /// </summary>
        /// <param name="rb">
        /// (layoutname device pagesize stylesheet scale)
        /// </param>
        /// <returns></returns>
        [_AcTrx.LispFunction("Plan2SetPlotSettings")]
        // ReSharper disable once UnusedMember.Global
        public static bool SetPlotSettings(_AcDb.ResultBuffer rb)
        {
            try
            {
                var arr = rb.AsArray();

                string loName, device, pageSize, styleSheet;
                double? scaleNumerator, scaleDenominator;
                short? rotation;

                TolerantGet(arr, out loName, 1);
                TolerantGet(arr, out device, 2);
                TolerantGet(arr, out pageSize, 3);
                TolerantGet(arr, out styleSheet, 4);
                TolerantGet(arr, out scaleNumerator, 5);
                TolerantGet(arr, out scaleDenominator, 6);
                TolerantGet(arr, out rotation, 7);

                return SetPlotSettings(loName, device, pageSize, styleSheet, scaleNumerator, scaleDenominator, rotation);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return false;
            }
        }

        /// <summary>
        /// Get plot settings -> not used yet
        /// </summary>
        /// <param name="rb">
        /// layoutname 
        /// </param>
        /// <returns></returns>
        [_AcTrx.LispFunction("Plan2GetPlotSettings")]
        // ReSharper disable once UnusedMember.Global
        public static _AcDb.ResultBuffer GetPlotSettings(_AcDb.ResultBuffer rb)
        {
            try
            {
                var arr = rb.AsArray();

                string loName, device, pageSize, styleSheet;
                double scaleNumerator, scaleDenominator;
                short rotation, scaleUnits;

                TolerantGet(arr, out loName, 0);
                //TolerantGet(arr, out device, 2);
                //TolerantGet(arr, out pageSize, 3);
                //TolerantGet(arr, out styleSheet, 4);
                //TolerantGet(arr, out scaleNumerator, 5);
                //TolerantGet(arr, out scaleDenominator, 6);
                //TolerantGet(arr, out rotation, 7);

                GetPlotSettings(loName, out device, out pageSize, out styleSheet, out scaleUnits, out scaleNumerator, out scaleDenominator, out rotation);

                _AcDb.ResultBuffer rbRet = new _AcDb.ResultBuffer
                {
	                new _AcDb.TypedValue((int) _AcBrx.LispDataType.Text, device),
	                new _AcDb.TypedValue((int) _AcBrx.LispDataType.Text, pageSize),
	                new _AcDb.TypedValue((int) _AcBrx.LispDataType.Text, styleSheet),
	                new _AcDb.TypedValue((int) _AcBrx.LispDataType.Int16, scaleUnits),
	                new _AcDb.TypedValue((int) _AcBrx.LispDataType.Double, scaleNumerator),
	                new _AcDb.TypedValue((int) _AcBrx.LispDataType.Double, scaleDenominator),
	                new _AcDb.TypedValue((int) _AcBrx.LispDataType.Int16, rotation)
                };

                return rbRet;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                return null;
            }
        }

        private static void GetPlotSettings(string loName, out string device, out string pageSize, out string styleSheet, out short scaleUnits, out double scaleNumerator, out double scaleDenominator, out short rotation)
        {
            _AcAp.Document doc = _AcAp.Application.DocumentManager.MdiActiveDocument;
            _AcDb.Database db = doc.Database;
            _AcDb.LayoutManager layoutMgr = _AcDb.LayoutManager.Current;

            var layoutId = layoutMgr.GetLayoutId(loName);
            if (!layoutId.IsValid) throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Layout '{0}' existiert nicht!", loName));

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var lay = (_AcDb.Layout)tr.GetObject(layoutId, _AcDb.OpenMode.ForRead);

                using (var ps = new _AcDb.PlotSettings(lay.ModelType))
                {
                    ps.CopyFrom(lay);
                    device = ps.PlotConfigurationName;
                    pageSize = ps.CanonicalMediaName;
                    styleSheet = ps.CurrentStyleSheet;
                    scaleUnits = (short)ps.PlotPaperUnits;
                    var scale = ps.CustomPrintScale;
                    scaleNumerator = scale.Numerator;
                    scaleDenominator = scale.Denominator;
                    var rot = ps.PlotRotation;
                    rotation = 0;
                    switch (rot)
                    {
                        case _AcDb.PlotRotation.Degrees090:
                            rotation = 90;
                            break;
                        case _AcDb.PlotRotation.Degrees180:
                            rotation = 180;
                            break;
                        case _AcDb.PlotRotation.Degrees270:
                            rotation = 270;
                            break;
                    }
                }

                tr.Commit();
            }
        }

        private static bool SetPlotSettings(string loName, string device, string pageSize, string styleSheet, double? scaleNumerator, double? scaleDenominator, short? plotRotation)
        {
            _AcAp.Document doc = _AcAp.Application.DocumentManager.MdiActiveDocument;
            _AcDb.Database db = doc.Database;
            _AcDb.LayoutManager layoutMgr = _AcDb.LayoutManager.Current;

            var layoutId = layoutMgr.GetLayoutId(loName);
            if (!layoutId.IsValid) throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Layout '{0}' existiert nicht!", loName));

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var layout = (_AcDb.Layout)tr.GetObject(layoutId, _AcDb.OpenMode.ForWrite);

                layout.SetPlotSettings(device, pageSize, styleSheet, scaleNumerator, scaleDenominator, plotRotation);

                tr.Commit();
            }

            return true;
        }

        private static void TolerantGet(_AcDb.TypedValue[] arr, out string val, int index)
        {
            val = null;

            try
            {
                if (arr.Length > index)
                {
                    var tp = arr[index];
                    if (tp.TypeCode != (short)_AcBrx.LispDataType.Nil) val = tp.Value.ToString();
                }
            }
            catch (Exception)
            {
	            // ignored
            }
        }
        private static void TolerantGet(_AcDb.TypedValue[] arr, out double? val, int index)
        {
            val = null;

            try
            {
                if (arr.Length > index)
                {
                    var tp = arr[index];
                    if (tp.TypeCode != (short)_AcBrx.LispDataType.Nil) val = (double)tp.Value;
                }
            }
            catch (Exception)
            {
	            // ignored
            }
        }
        private static void TolerantGet(_AcDb.TypedValue[] arr, out short? val, int index)
        {
            val = null;

            try
            {
                if (arr.Length > index)
                {
                    var tp = arr[index];
                    if (tp.TypeCode != (short)_AcBrx.LispDataType.Nil) val = (short)tp.Value;
                }
            }
            catch (Exception)
            {
	            // ignored
            }
        }
    }
}
