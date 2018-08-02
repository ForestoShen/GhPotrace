using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using CsPotrace;
using System.Drawing;
using Grasshopper;

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
          : base("Rooster", "Trace",
              "raster to vector",
              "Params", "Util")
        {
        }
        string ImgPath;
        Bitmap bm;
        bool treeOutput = false;
        //Rectangle3d boundary = new Rectangle3d();


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "Path", "image path", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Threshold", "Tresh", "Threshold for binary op, 0-100(%)", GH_ParamAccess.item, 50);
            pManager.AddNumberParameter("CornorT", "CT", "cornor alpha multiplier", GH_ParamAccess.item, 1.0);
            pManager.AddIntegerParameter("TurnPolicy", "TP", "Turn Policy: 0 - minority,1 - majority,2 - right,3 - black,4 - white", GH_ParamAccess.item,0);
            pManager.AddIntegerParameter("TurdSize", "Size", "ignore Curve whose coverage < turdsize unit", GH_ParamAccess.item, 2);
            pManager.AddBooleanParameter("Optimize", "Op", "optimize curve or not", GH_ParamAccess.item,true);
            pManager.AddNumberParameter("Tolerance", "Tol", "tolerance for optimization", GH_ParamAccess.item,0.2);
            //pManager.AddRectangleParameter("Boundary", "Bound", "Image boundary", GH_ParamAccess.item, Rectangle3d.Unset);
            pManager.AddBooleanParameter("Invert", "Inv", "Invert Image color", GH_ParamAccess.item, false);

        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "Crvs", "Curve segments", GH_ParamAccess.list);
            pManager.AddRectangleParameter("Boundary", "Bound", "Inital Rectangle boundaray", GH_ParamAccess.item);
            //pManager.AddTextParameter("out", "out", "output", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            DA.GetData(0, ref ImgPath);
            int t = 0;
            DA.GetData(1, ref t);
            Potrace.Treshold = (double)t / 100;
            DA.GetData(2, ref Potrace.alphamax);
            int p = 0;
            DA.GetData(3, ref p);
            Potrace.turnpolicy = (TurnPolicy)p;
            DA.GetData(4, ref Potrace.turdsize);
            
            DA.GetData(5, ref Potrace.curveoptimizing);
            DA.GetData(6, ref Potrace.opttolerance);
            
            //DA.GetData(7, ref boundary);
            bool inv = false;
            DA.GetData(7, ref inv);

            bm = new Bitmap(ImgPath);
            bm.RotateFlip(RotateFlipType.RotateNoneFlipY);

            // convert png transparent background to white
            var b = new Bitmap(bm.Width, bm.Height);
            b.SetResolution(bm.HorizontalResolution, bm.VerticalResolution);
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.White);
                g.DrawImageUnscaled(bm, 0, 0);
            }

            double H = bm.Height;
            double W = bm.Width;
            Rectangle3d boundary = new Rectangle3d(Plane.WorldXY, W, H);
            /*
            // phsical size
            double bh = boundary.Y.Length;
            double bw = boundary.X.Length;
            // scale Factor
            double fh = bh / H;
            double fw = bw / W;
            */
            if (treeOutput)
            {
                DataTree<Curve> crvs = new DataTree<Curve>();
                Potrace.Potrace_Trace(b, crvs, inv);

                DA.SetDataTree(0, crvs);
            }
            else
            {
                List<Curve> crvs = new List<Curve>();
                Potrace.Potrace_Trace(b, crvs, inv);

                DA.SetDataList(0, crvs);
            }

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
            var m = Menu_AppendItem(menu, "Tree Output", ChangeMode,true,treeOutput);
            m.ToolTipText = "If checked, will output unjoined crvs as tree.";
        }
        

        private void ChangeMode(object sender, EventArgs e)
        {
        RecordUndoEvent("Mode Change");
        treeOutput = !treeOutput; 
        ExpireSolution(true);
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
            get { return new Guid("7c4c9e66-8ac0-4ff9-8756-b02f4f90bb9b"); }
        }
    }
}
