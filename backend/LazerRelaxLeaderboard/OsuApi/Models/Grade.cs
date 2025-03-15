using System.ComponentModel;

namespace LazerRelaxLeaderboard.OsuApi.Models;

public enum Grade
{
    [Description(@"F")]
    F,

    [Description(@"D")]
    D,

    [Description(@"C")]
    C,

    [Description(@"B")]
    B,

    [Description(@"A")]
    A,

    [Description(@"S")]
    S,

    [Description(@"S")]
    SH,

    [Description(@"SS")]
    X,

    [Description(@"SS")]
    XH
}
