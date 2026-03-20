---
layout: default
title: الإعداد
permalink: /ar/setup/
lang: ar
rtl: true
---

# إعداد المشروع (عربي — ملخص)

للتفاصيل الكاملة راجع **[Setup بالإنجليزية](../setup.md)**.

## المتطلبات الأساسية

- **Unity 6** (الإصدار المذكور في `README` أو `ProjectVersion.txt` داخل المشروع).
- استنساخ المستودع وفتح مجلد المشروع الذي يحتوي مجلد **`Assets`** (وليس مجلد الجذر العام إن وُجد لبّنة فوقه).

## وثائق الموقع (GitHub Pages)

مجلد **`docs`** يبني موقع **Jekyll**. للمعاينة محلياً:

```bash
cd docs
bundle install
bundle exec jekyll serve
```

ثم افتح العنوان الذي يطبعَه Jekyll (غالباً مع `/First-Principles/` حسب `baseurl` في `_config.yml`).

[← العودة للرئيسية العربية]({% link ar/index.md %})
