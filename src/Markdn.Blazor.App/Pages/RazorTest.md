---
title: Razor Syntax Test
namespace: Markdn.Blazor.App.Pages
---

# Razor Syntax Preservation Test

This page demonstrates Razor syntax preservation through Markdown processing.

## @code Block Test

@code {
    private int counter = 0;
    private string message = "Hello from Razor!";
    
    private void IncrementCounter()
    {
        counter++;
    }
}

## Expression Test

Current time: @DateTime.Now

Message: @message

Counter value: @(counter * 2)

## Component Test

<Counter />

## Mixed Content

You can mix **Markdown** with Razor expressions seamlessly.

- List item with @message
- Another item with @DateTime.Now.ToString()
