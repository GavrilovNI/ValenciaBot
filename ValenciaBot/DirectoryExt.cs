using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValenciaBot.DirectoryExtensions;
public static class DirectoryExt
{
    public static DirectoryInfo EnvironmentDirectory => new(Environment.CurrentDirectory);
    public static DirectoryInfo? ProjectDirectory => EnvironmentDirectory.Parent?.Parent?.Parent;
}
