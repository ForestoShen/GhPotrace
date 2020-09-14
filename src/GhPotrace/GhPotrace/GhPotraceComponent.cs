﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using CsPotrace;
using System.Drawing;
using Grasshopper;
using ShapeDiver.Public.Grasshopper.Parameters;
using System.Windows.Forms;
using GH_IO.Serialization;
using nQuant;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using System.Drawing.Imaging;

// Modified for Compatibility with ShapeDiver's Grasshopper Bitmap Parameter and Support on the ShapeDiver Platform, along with general Improvements and Bugfixes
// Original author: Foresto Shen
// https://github.com/ForestoShen/GhPotrace
// License: GNU
// Modified by Praneet Mathur for ShapeDiver GmbH
// https://shapediver.com/

namespace GhPotrace
{
    public class GhPotraceComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public GhPotraceComponent()
          : base("Rooster", "Rooster",
              "Raster Image to Vector Curves. Modified for compatibility with ShapeDiver Platform.",
              "Params", "Util")
        {
        }

        bool getColors = true;
        ToolStripMenuItem tp;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddParameter(new GrasshopperBitmapParam(), "Image Bitmap", "Bitmap", "Image Bitmap", GH_ParamAccess.item);
            Params.Input[pManager.AddNumberParameter("Threshold", "T", "Threshold for curve detection; 0.0 to 100.0%", GH_ParamAccess.item, 50.0)].Optional = true;
            Params.Input[pManager.AddNumberParameter("Corner Threshold", "CT", "Corner Alpha Multiplier; 0.0 to 1.0", GH_ParamAccess.item, 1.0)].Optional = true;
            Params.Input[pManager.AddNumberParameter("Max Turd Area", "MTA", "Curves with area larger than Max Turd Size will be ignored", GH_ParamAccess.item, 2)].Optional = true;
            Params.Input[pManager.AddBooleanParameter("Optimize", "O", "Optimize Curves", GH_ParamAccess.item, true)].Optional = true;
            Params.Input[pManager.AddNumberParameter("Tolerance", "TO", "Tolerance for Optimization; 0.0 to 1.0", GH_ParamAccess.item, 0.2)].Optional = true;
            Params.Input[pManager.AddBooleanParameter("Invert", "I", "Invert Image Colors (if getting colors enabled) or Invert Turd Detection (if getting colors disabled)", GH_ParamAccess.item, false)].Optional = true;
            Params.Input[pManager.AddIntegerParameter("Color Count", "C", "Color Count for Quantization", GH_ParamAccess.item, 2)].Optional = true;
            //pManager.AddTextParameter("Path", "Path", "image path", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("TurnPolicy", "TP", "Turn Policy: 0 - minority,1 - majority,2 - right,3 - black,4 - white", GH_ParamAccess.item,0);
            // Note: Moved ^turn policy^ to Right-click Menu Items
            //pManager.AddRectangleParameter("Boundary", "Bound", "Image boundary", GH_ParamAccess.item, Rectangle3d.Unset);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Curve Segments", GH_ParamAccess.tree);
            pManager.AddRectangleParameter("Boundary", "B", "Rectangle boundary", GH_ParamAccess.item);
            pManager.AddColourParameter("Colors", "Col", "List of Colors for each Curve", GH_ParamAccess.tree);
            //pManager.AddTextParameter("out", "out", "output", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Variables
            Bitmap bm;
            GrasshopperBitmapGoo ghbm = new GrasshopperBitmapGoo();
            double t = 50.0;
            double a = 1.0;
            double mts = 2;
            bool opt = true;
            double opttol = 0.2;
            bool inv = false;
            int colorCount = 2;

            // Get Data from Input Params
            if (!DA.GetData(0, ref ghbm)) return;
            if (DA.GetData(1, ref t))
            {
                if (0.0 > t || t > 100.0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Threshold must lie between 0.0 to 100.0");
                    return;
                }
            }
            if (DA.GetData(2, ref a))
            {
                if (0.0 > a || a > 1.0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Corner Threshold must lie between 0.0 to 1.0");
                    return;
                }
            }
            DA.GetData(3, ref mts);
            DA.GetData(4, ref opt);
            if (DA.GetData(5, ref opttol))
            {
                if (0.0 > opttol || opttol > 1.0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tolerance for Optimization must lie between 0.0 to 1.0");
                    return;
                }
            }
            DA.GetData(6, ref inv);
            if (DA.GetData(7, ref colorCount))
            {
                if (colorCount < 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Color Count cannot be negative");
                    return;
                }
            }
            else
            {
                if (getColors)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Color Count not set. Setting 'Get Colors' to False. Please set a value for Color Count and re-enable Get Colors by right-clicking on the component");
                    getColors = false;
                }
            }

            // set Data in Potrace fields
            Potrace.Treshold = t / 100;
            Potrace.alphamax = a * (4 / 3);
            Potrace.turdsize = ((int)Math.Round(mts, 0, MidpointRounding.AwayFromZero));
            Potrace.curveoptimizing = opt;
            Potrace.opttolerance = opttol;

            if (ghbm.IsValid && ghbm.Image != null)
                bm = ghbm.Image;
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Bitmap");
                return;
            }

            // convert png transparent background to white
            if (!getColors || (colorCount == 0))
            {
                using (Bitmap b = new Bitmap(bm.Width, bm.Height))
                {
                    b.SetResolution(bm.HorizontalResolution, bm.VerticalResolution);
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.Clear(Color.White);
                        g.DrawImageUnscaled(bm, 0, 0);
                    }

                    b.RotateFlip(RotateFlipType.RotateNoneFlipY);

                    DataTree<Curve> crvs = new DataTree<Curve>();
                    Potrace.Potrace_Trace(b, crvs, inv);

                    DA.SetDataTree(0, crvs);
                }
            }
            else
            {
                using (Bitmap b = new Bitmap(bm.Width, bm.Height))
                {
                    b.SetResolution(bm.HorizontalResolution, bm.VerticalResolution);
                    using (Graphics g = Graphics.FromImage(b))
                    {
                        g.Clear(Color.Transparent);
                        g.DrawImageUnscaled(bm, 0, 0);
                    }
                    b.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    WuQuantizer quantizer = new WuQuantizer();
                    Bitmap quantized = (Bitmap)quantizer.QuantizeImage(b, colorCount + 1);
                    Color[] colors = new Color[colorCount];
                    Array.Copy(quantized.Palette.Entries, 0, colors, 0, colorCount);
                    DataTree<GH_Colour> colorsOut = new DataTree<GH_Colour>();
                    DataTree<Curve> crvs = new DataTree<Curve>();
                    for (int i = 0; i < colorCount; i++)
                    {
                        Bitmap temp = quantized.Clone(new Rectangle(0, 0, b.Width, b.Height), PixelFormat.Format32bppArgb);
                        var bmData = temp.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                        unsafe
                        {
                            byte* p = (byte*)bmData.Scan0;
                            int stopAddress = (int)p + bmData.Stride * bmData.Height;
                            while ((int)p != stopAddress)
                            {
                                if (p[0] == colors[i].B && p[1] == colors[i].G && p[2] == colors[i].R && p[3] == colors[i].A)
                                {
                                    p[0] = p[1] = p[2] = p[3] = 255;
                                }
                                else
                                {
                                    p[0] = p[1] = p[2] = p[3] = 0;
                                }
                                p += 4;
                            }
                            temp.UnlockBits(bmData);

                            List<Curve> curves = new List<Curve>();
                            Potrace.Potrace_Trace(temp, curves, true);
                            crvs.AddRange(curves, new GH_Path(i));
                            if (inv)
                            {
                                Color invCol = Color.FromArgb(((int)colors[i].A), (255 - ((int)colors[i].R)), (255 - ((int)colors[i].G)), (255 - ((int)colors[i].B)));
                                colorsOut.Add(new GH_Colour(invCol), new GH_Path(i));
                            }
                            else
                            {
                                colorsOut.Add(new GH_Colour(colors[i]), new GH_Path(i));
                            }
                            Potrace.Clear();
                            Potrace.Treshold = t / 100;
                            Potrace.alphamax = a * (4 / 3);
                            Potrace.turdsize = ((int)Math.Round(mts, 0, MidpointRounding.AwayFromZero));
                            Potrace.curveoptimizing = opt;
                            Potrace.opttolerance = opttol;
                        }
                    }
                    DA.SetDataTree(0, crvs);
                    DA.SetDataTree(2, colorsOut);
                }
            }
            Rectangle3d boundary = new Rectangle3d(Plane.WorldXY, (double)bm.Width, (double)bm.Height);
            DA.SetData(1, boundary);
        }
        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            Potrace.Clear();
        }

        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            var m = Menu_AppendItem(menu, "Get Colors", ChangeMode, true, getColors);
            m.ToolTipText = "If checked, will output unjoined curves in a tree data structure.";

            tp = Menu_AppendItem(menu, "Turn Policy", null, true);
            tp.DropDownItems.Add(new ToolStripMenuItem("Minority", null, ChangeTurnPolicy));
            tp.DropDownItems.Add(new ToolStripMenuItem("Majority", null, ChangeTurnPolicy));
            tp.DropDownItems.Add(new ToolStripMenuItem("Right", null, ChangeTurnPolicy));
            tp.DropDownItems.Add(new ToolStripMenuItem("Black", null, ChangeTurnPolicy));
            tp.DropDownItems.Add(new ToolStripMenuItem("White", null, ChangeTurnPolicy));
            ((ToolStripMenuItem)tp.DropDownItems[(int)Potrace.turnpolicy]).Checked = true;
        }        

        private void ChangeMode(object sender, EventArgs e)
        {
            RecordUndoEvent("Mode Change");
            getColors = !getColors;
            if (getColors) Params.Output[1].Access = GH_ParamAccess.tree;
            else Params.Output[1].Access = GH_ParamAccess.list;
            ExpireSolution(true);
        }

        private void ChangeTurnPolicy(object sender, EventArgs e)
        {
            RecordUndoEvent("Turn Policy Change");
            ToolStripMenuItem menuitem = (ToolStripMenuItem)sender;
            if (!menuitem.Checked)
            {
                switch (menuitem.Text)
                {
                    case "Minority":
                        {
                            Potrace.turnpolicy = TurnPolicy.minority;
                            break;
                        }
                    case "Majority":
                        {
                            Potrace.turnpolicy = TurnPolicy.majority;
                            break;
                        }
                    case "Right":
                        {
                            Potrace.turnpolicy = TurnPolicy.right;
                            break;
                        }
                    case "Black":
                        {
                            Potrace.turnpolicy = TurnPolicy.black;
                            break;
                        }
                    case "White":
                        {
                            Potrace.turnpolicy = TurnPolicy.white;
                            break;
                        }
                    default: break;
                }
                foreach (ToolStripMenuItem o in tp.DropDownItems)
                {
                    if (o.Text != menuitem.Text)
                        o.Checked = false;
                    else
                        o.Checked = true;
                }
                ExpireSolution(true);
            }            
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.SetBoolean("RoosterOutputMode", getColors);
            writer.SetInt32("RoosterTurnPolicy", (int)Potrace.turnpolicy);
            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            if (!reader.TryGetBoolean("RoosterOutputMode", ref getColors)) getColors = false;
            int tpread = 0;
            if (reader.TryGetInt32("RoosterTurnPolicy", ref tpread)) Potrace.turnpolicy = (TurnPolicy)tpread;
            else Potrace.turnpolicy = TurnPolicy.minority;
            return base.Read(reader);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Properties.Resources.res;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("cdc68afa-0133-41fe-b879-43f54c1d9885"); }
        }
    }
}
