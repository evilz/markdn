---
slug: /component-params
title: Component Parameters Test
componentNamespaces:
	- Markdn.Blazor.App.Components
	- Markdn.Blazor.App.Components.Shared
	- Markdn.Blazor.App.Components.Pages
---

# Component Parameters Test

This page demonstrates passing parameters to components.

## Alert Component with Parameters

<Alert Severity="Warning">This is a warning message!</Alert>

<Alert Severity="Success">Operation completed successfully!</Alert>

<Alert Severity="Danger">Error: Something went wrong!</Alert>

## Mixed Content

You can mix Markdown **formatting** with components:

<Alert Severity="Info">
Check out this **important** information with *emphasis*!
</Alert>
