---
title: Banner
description: Display a global banner at the top of your site.
icon: megaphone
---

# Banner

The Banner component allows you to display a global announcement or call-to-action at the very top of your site, above the navigation bar. This is perfect for announcements, promotions, or important notices.

## Configuration

The banner is configured globally in your `neko.yml` file.

```yaml
banner:
  text: "Big news! We're excited to announce Neko 2.0."
  link: /blog/neko-2-0
  linkText: "Read the announcement"
  visible: true
  background: bg-indigo-600
  color: text-white
  id: announcement-v2
  dismissible: true
```

## Properties

| Property | Type | Default | Description |
| :--- | :--- | :--- | :--- |
| `text` | `string` | - | The main text message to display in the banner. |
| `link` | `string` | - | An optional URL for the call-to-action button/link. |
| `linkText` | `string` | "Read more" | The text for the call-to-action link. |
| `visible` | `bool` | `true` | Whether the banner should be rendered. |
| `background` | `string` | `bg-indigo-600` | Tailwind CSS classes for the banner background. |
| `color` | `string` | `text-white` | Tailwind CSS classes for the text color. |
| `id` | `string` | `global-banner` | A unique identifier for the banner. This is used to persist the dismissed state. Change this ID to show the banner again to users who have dismissed it. |
| `dismissible` | `bool` | `true` | Whether to show a close button that allows users to dismiss the banner. |

## Examples

### Standard Announcement

```yaml
banner:
  text: "We've just released a new feature!"
  link: /changelog
  linkText: "Check it out"
```

### Warning Banner

```yaml
banner:
  text: "Scheduled maintenance on Sunday at 2 AM UTC."
  background: bg-yellow-500
  color: text-yellow-900
  dismissible: false
  id: maintenance-notice
```

### Simple Text

```yaml
banner:
  text: "Welcome to our documentation!"
  dismissible: true
```

## Dismissal Logic

When a user dismisses the banner, a flag is stored in their browser's `localStorage` using the key `banner-dismissed-{id}`. To show the banner again (e.g., for a new announcement), simply change the `id` property in your configuration.
