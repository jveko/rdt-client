﻿namespace RdtClient.Data.Models.Internal;

public class SettingProperty
{
    public String Key { get; set; }
    public Object Value { get; set; }
    public String DisplayName { get; set; }
    public String Description { get; set; }
    public String Type { get; set; }
    public Dictionary<Int32, String> EnumValues { get; set; }
}