﻿namespace Client.Queries;

public interface IConstraintWithSuffix
{
    string? SuffixIfApplied { get; }

    bool ArgumentImplicitForSuffix { get; }
}