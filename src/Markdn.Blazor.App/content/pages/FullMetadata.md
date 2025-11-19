---
url: /full-metadata
title: Full Metadata Test Page
variables:
    invoice: 34843
    date   : 2001-01-23
    bill-to: &id001
        given  : Chris
        family : Dumars
        address:
            lines: |
                458 Walkman Dr.
                Suite #292
            city    : Royal Oak
            state   : MI
            postal  : 48046
    ship-to: *id001
    product:
        - sku         : BL394D
          quantity    : 4
          description : Basketball
          price       : 450.00
        - sku         : BL4438H
          quantity    : 1
          description : Super Hoop
          price       : 2392.00
tax  : 251.42
total: 4443.52
comments:
    Late afternoon is best.
    Backup contact is Nancy
    Billsmer @ 338-4338.
    

parameters:
    pageSize: 10       # → [Parameter] public int PageSize { get; set; } = 10;
    enabled: false     # → [Parameter] public bool Enabled { get; set; } = false;
---

# Full Metadata Test

This page tests all YAML front matter configuration options.

## Variables

invoice: @invoice
date: @date
bill to:

- given: @billTo.given
- family: @billTo.family
- address lines:
  -  @billTo.address.lines
