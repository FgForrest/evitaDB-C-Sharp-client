﻿namespace Client.Models.Data;

public interface IEntityClassifierWithParent : IEntityClassifier
{
    IEntityClassifierWithParent? ParentEntity { get; }
}