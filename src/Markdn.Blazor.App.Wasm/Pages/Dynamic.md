---
url: /dynamic
title: Dynamic Component (Hot Reload Test)
---

# Dynamic Content Example - UPDATED

Welcome! The current time is: @DateTime.Now.ToString("HH:mm:ss")

Current date: @DateTime.Now.ToString("yyyy-MM-dd")

## Name Display

Hello, @name! Welcome to hot reload testing.

@code {
    private string name = "World";
    
    protected override void OnInitialized()
    {
        name = "Blazor Developer";
    }
}
