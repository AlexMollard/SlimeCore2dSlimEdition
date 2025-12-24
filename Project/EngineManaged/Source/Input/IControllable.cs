using System;
using System.Collections.Generic;
using System.Text;

namespace SlimeCore.Source.Input;
/// <summary>
/// An Entity/Object which can recieve input commands
/// </summary>
public interface IControllable
{
    public void RecieveInput(bool IgnoreInput);
}
