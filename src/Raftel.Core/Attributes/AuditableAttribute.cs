namespace Raftel.Core.Attributes;

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class AuditableAttribute : Attribute;