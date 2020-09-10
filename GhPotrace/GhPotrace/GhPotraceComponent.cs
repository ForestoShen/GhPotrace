using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel;
using Rhino.Geometry;
using CsPotrace;
using System.Drawing;
using Grasshopper;
using nQuant;
using System.Drawing.Imaging;
using Grasshopper.Kernel.Types;

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
          : base("Rooster new", "Trace new",
              "raster to vector",
              "Params", "Util")
        {
        }
        string ImgPath;
        Bitmap bm;
        bool parallel = false;
        //Rectangle3d boundary = new Rectangle3d();


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "Path", "image path", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Threshold", "Tresh", "Threshold for binary op, 0-100(%)", GH_ParamAccess.item, 50);
            pManager.AddNumberParameter("CornorT", "CT", "Cornor threshold,0.0-1.3334, a value of 0.0 only produce polylines, value over 1.3334 only produce curves(no corner)", GH_ParamAccess.item, 1.0);
            pManager.AddIntegerParameter("TurnPolicy", "TP", "Turn Policy, determine how corner point should be vectorized: 0 - minority,1 - majority,2 - right,3 - black,4 - white. see http://potrace.sourceforge.net/potracelib.pdf for details", GH_ParamAccess.item,0);
            pManager.AddIntegerParameter("TurdSize", "Size", "Ignore Curve whose area < turdsize unit", GH_ParamAccess.item, 2);
            pManager.AddBooleanParameter("Optimize", "Op", "Optimize curve or not?", GH_ParamAccess.item,true);
            pManager.AddNumberParameter("Tolerance", "Tol", "Tolerance for optimization", GH_ParamAccess.item,0.2);
            //pManager.AddRectangleParameter("Boundary", "Bound", "Image boundary", GH_ParamAccess.item, Rectangle3d.Unset);
            pManager.AddBooleanParameter("Invert", "Inv", "Invert Image color", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("ColorCount", "CC", "ColorCount for quantization", GH_ParamAccess.item);
            pManager[8].Optional = true;

        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "Crvs", "Curve segments", GH_ParamAccess.tree);
            pManager.AddRectangleParameter("Boundary", "Bound", "Inital Rectangle boundaray", GH_ParamAccess.item);
            pManager.AddColourParameter("Colors", "clr", "Color for each segment", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            DA.GetData(0, ref ImgPath);
            int t = 0;
            DA.GetData(1, ref t);
            Potrace.Treshold = (double)t / 100;
            DA.GetData(2, ref Potrace.alphamax);
            int policy = 0;
            DA.GetData(3, ref policy);
            Potrace.turnpolicy = (TurnPolicy)policy;
            DA.GetData(4, ref Potrace.turdsize);
            
            DA.GetData(5, ref Potrace.curveoptimizing);
            DA.GetData(6, ref Potrace.opttolerance);
            
            //DA.GetData(7, ref boundary);
            bool inv = false;
            DA.GetData(7, ref inv);
            int count = 0;
            DA.GetData(8, ref count);
            //Read and flip image
            bm = new Bitmap(ImgPath);
            bm.RotateFlip(RotateFlipType.RotateNoneFlipY);
            // convert to argb
            bm = bm.Clone(new Rectangle(0, 0, bm.Width, bm.Height), PixelFormat.Format32bppArgb);
            // get boundary
            int H = bm.Height;
            int W = bm.Width;
            Rectangle3d boundary = new Rectangle3d(Plane.WorldXY, W, H);

            DataTree<GH_Colour> GC = new DataTree<GH_Colour>();
            DataTree<Curve> curves = new DataTree<Curve>();
            if ((int)count < 1) {
                // convert png transparent background to white
                var b = new Bitmap(bm.Width, bm.Height);
                b.SetResolution(bm.HorizontalResolution, bm.VerticalResolution);
                using (var g = Graphics.FromImage(b)) {
                    g.Clear(Color.White);
                    g.DrawImageUnscaled(bm, 0, 0);
                }
                List<Curve> crvs = new List<Curve>();
                Potrace.Potrace_Trace(b, crvs, inv);
                curves.AddRange(crvs);
            }
            else {
                // quantitize image
                Potrace.Treshold = 0.1;
                var quantizer = new WuQuantizer();
                Bitmap quantized = (Bitmap)quantizer.QuantizeImage(bm, count + 1);
                Color[] colors = new Color[count];
                Array.Copy(quantized.Palette.Entries, 0, colors, 0, count);
                // segment image by color
                // TODO, processing each color in parellel
                if (parallel) {
                    for (int i = 0; i < colors.Length; i++) {
                        Bitmap temp = quantized.Clone(new Rectangle(0, 0, bm.Width, bm.Height), PixelFormat.Format32bppArgb);
                        var bmData = temp.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                        unsafe
                        {
                            byte* p = (byte*)bmData.Scan0;
                            int stopAddress = (int)p + bmData.Stride * bmData.Height;
                            while ((int)p != stopAddress) {
                                if (p[0] == colors[i].B && p[1] == colors[i].G && p[2] == colors[i].R && p[3] == colors[i].A) {
                                    p[0] = p[1] = p[2] = p[3] = 255;
                                }
                                else {
                                    p[0] = p[1] = p[2] = p[3] = 0;  
                                }
                                p += 4;
                            }
                            temp.UnlockBits(bmData);

                            List<Curve> crvs = new List<Curve>();
                            Potrace.Potrace_Trace(temp, crvs, inv);
                            curves.AddRange(crvs, new GH_Path(i));
                            GC.Add(new GH_Colour(colors[i]), new GH_Path(i));
                            Potrace.Clear();
                        }

                    }

                }
                for (int i = 0; i < colors.Length; i++) {
                    Bitmap temp = quantized.Clone(new Rectangle(0, 0, bm.Width, bm.Height), PixelFormat.Format32bppArgb);
                    var bmData = temp.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                    unsafe
                    {
                        byte* p = (byte*)bmData.Scan0;
                        int stopAddress = (int)p + bmData.Stride * bmData.Height;
                        while ((int)p != stopAddress) {
                            if (p[0] == colors[i].B && p[1] == colors[i].G && p[2] == colors[i].R && p[3] == colors[i].A) {
                                p[0] = p[1] = p[2] = p[3] = 255;
                            }
                            else {
                                p[0] = p[1] = p[2] = p[3] = 0;
                            }
                            p += 4;
                        }
                        temp.UnlockBits(bmData);

                        List<Curve> crvs = new List<Curve>();
                        Potrace.Potrace_Trace(temp, crvs, inv);
                        curves.AddRange(crvs, new GH_Path(i));
                        GC.Add(new GH_Colour(colors[i]), new GH_Path(i));
                        Potrace.Clear();
                    }

                }
            }
            DA.SetDataTree(0, curves);
            DA.SetData(1, boundary);
            DA.SetDataTree(2, GC);



        }
        protected override void BeforeSolveInstance()
        {
            base.BeforeSolveInstance();
            Potrace.Clear();

        }
        protected override void AfterSolveInstance()
        {
            base.AfterSolveInstance();
            Potrace.Clear();

        }
        
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            var m = Menu_AppendItem(menu, "Parallel", ChangeMode,true, parallel);
            m.ToolTipText = "TODO, parallel color quantizier.";
        }


        private void ChangeMode(object sender, EventArgs e)
        {
            RecordUndoEvent("Mode Change");
            parallel = !parallel;
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
            get { return new Guid("1F283E66-FA65-4CBC-8812-58DEF2365518"); }
        }
    }
}
