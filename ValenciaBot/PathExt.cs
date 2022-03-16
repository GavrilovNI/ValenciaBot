using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValenciaBot.PathExtensions;

public static class PathExt
{
    public static string SetName(string? path, string name)
    {
        string? dir = System.IO.Path.GetDirectoryName(path);
        string ext = System.IO.Path.GetExtension(path) ?? "";

        if(dir is null)
            path = name + ext;
        else
            path = System.IO.Path.Combine(dir, name + ext);

        return path;
    }
}
