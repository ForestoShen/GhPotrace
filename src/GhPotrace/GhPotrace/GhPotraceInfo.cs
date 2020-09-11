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
                return "GhPotrace [ShapeDiver Compatible Version]";
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
                return "Vectorize a bitmap to curves. This version of Rooster / GhPotrace is modified to be supported on the ShapeDiver Platform.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("876df797-a78a-49ca-b69d-2c055600dcf9");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Foresto Shen and ShapeDiver GmbH";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "847943216@qq.com and https://www.shapediver.com/";
            }
        }

        public override string Version
        {
            get
            {
                return "1.1.0";
            }
        }

        public override string AssemblyName => this.Name;

        public override string AssemblyDescription => this.Description;

        public override GH_LibraryLicense AssemblyLicense => this.License;

        public override string AssemblyVersion => this.Version;

        public override Bitmap AssemblyIcon => Properties.Resources.res;

        public override GH_LibraryLicense License => GH_LibraryLicense.opensource;
    }
}
