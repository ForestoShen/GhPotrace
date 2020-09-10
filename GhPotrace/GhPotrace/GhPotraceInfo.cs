using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GhPotrace
{
    public class GhPotraceInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "GhPotrace";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Properties.Resources.res;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Vectorize a bitmap to nurbs curve, which consist of Polyline and BezierCurve";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("1F283E66-FA65-4CBC-8812-58DEF2365518");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Foresto Shen";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "847943216@qq.com";
            }
        }
    }
}
