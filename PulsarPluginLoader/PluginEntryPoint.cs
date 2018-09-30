﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PulsarPluginLoader
{
    /// <summary>
    /// Designates the entry point method for a PULSAR plugin.
    /// 
    /// Entry point methods should be public, static, void, and take no arguments
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PluginEntryPoint : Attribute { }
}