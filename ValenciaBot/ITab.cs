using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValenciaBot;
public interface ITab
{
    public bool Opened { get; }

    public void Open();
    public void Close();
    public void Reload();
}
