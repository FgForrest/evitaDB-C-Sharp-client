﻿namespace Client.Queries;

public interface IConstraintVisitor
{
    void Visit(IConstraint constraint);
}