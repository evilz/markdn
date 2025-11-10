---
url: /dynamic
title: Dynamic Component
---

# Dynamic Content Example

Welcome! The current time is: @DateTime.Now.ToString("HH:mm:ss")

## Name Display

Hello, @name!

@code {
    private string name = "World";
    
    protected override void OnInitialized()
    {
        name = "Blazor Developer";
    }
}
