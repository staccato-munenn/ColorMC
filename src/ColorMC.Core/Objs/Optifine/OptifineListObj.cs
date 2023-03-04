﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ColorMC.Core.Objs.Optifine;

public record OptifineListObj
{
    public string _id { get; set; }
    public string mcversion { get; set; }
    public string patch { get; set; }
    public string type { get; set; }
    public string filename { get; set; }
    public string forge { get; set; }
}
