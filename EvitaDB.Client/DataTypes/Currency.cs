﻿namespace EvitaDB.Client.DataTypes;

public class Currency
{
    public string CurrencyCode { get; }
    
    public Currency(string currencyCode)
    {
        CurrencyCode = currencyCode;
    }

    public override string ToString()
    {
        return $"[{CurrencyCode}]";
    }
}