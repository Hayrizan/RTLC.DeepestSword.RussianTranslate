# Deepest Sword — Русификатор (BepInEx 5)

Плагин-русификатор для игры **Deepest Sword** на базе **BepInEx 5 (x64)** с пиксельным кириллическим шрифтом и русифицированным логотипом игры.

## Установка

1. Скачайте `RTLC.DeepestSword.RussianTranslate.zip` из [последнего релиза](https://github.com/Hayrizan/RTLC.DeepestSword.RussianTranslate/releases/latest). BepInEx 5.4.23.6 для Windows x64 уже в архиве.
2. Закройте игру и распакуйте содержимое ZIP прямо в папку с `Deepest Sword.exe`. В Steam её можно открыть через **Управление → Просмотреть локальные файлы**.
3. Запустите игру через Steam.

Для удаления перевода уберите папку `BepInEx/plugins/RTLC.DeepestSword.RussianTranslate`. Если перевод не загрузился, проверьте `BepInEx/LogOutput.log`.

## Что переведено

- Весь интерфейс (титульный экран, HUD, экран победы и рекордов, меню паузы, настройки управления, камеры и звука, режим спидрана, экран авторов).
- Все игровые подсказки и обучения в мире.
- Все диалоги персонажей (Кузнец, Искатель приключений, Дракон).
- Пиксель-арт логотип игры на титульном экране (**«ДЛИННЕЙШИЙ МЕЧ»**) и финальная иллюстрация (**«Ты пришёл за мной!»**).
- Пиксельный шрифт `pixelFJ8pt1__` расширен атласом `1024×1024` в оригинальном стиле с полной поддержкой кириллицы (`А–Я`, `Ё`, `а–я`, `ё`) и типографики (`«»`, `—`, `–`, `…`, `№`).
- Переводы, текстуры и данные кириллического шрифта загружаются из `rtlc.bundle`.

## Структура проекта

- `src/RTLC.DeepestSword.RussianTranslate/` — исходный код BepInEx-плагина (`netstandard2.0`) и исходные ресурсы для бандла (`Assets/`).
- `tools/AssetBundleProject/` — минимальный Unity-проект и скрипт сборки AssetBundle.
- `tools/generate_assets.py` — генератор расширенного атласа шрифта и русифицированных текстур `Logo.png` и `ending.png`.

## Сборка

Понадобятся .NET SDK, Unity Editor **2021.1.4f1** и установленная игра с BepInEx 5. Затем выполните из корня проекта:

```powershell
.\tools\build.ps1 -GameDir "C:\Games\Deepest Sword"
```

Скрипт собирает Windows x64 AssetBundle, DLL и готовую папку:

```text
dist/RTLC.DeepestSword.RussianTranslate/
├── RTLC.DeepestSword.RussianTranslate.dll
└── rtlc.bundle
```

Если Unity Editor установлен в нестандартной папке, передайте путь к `Editor/Unity.exe` через `-UnityEditorPath`. Для сборки DLL отдельно по-прежнему можно использовать `dotnet build -c Release "-p:GameDir=C:\Games\Deepest Sword"`.

## Настройка

После первого запуска конфигурационный файл создаётся по пути:
`Deepest Sword\BepInEx\config\RTLC.DeepestSword.RussianTranslate.cfg`

- `Enabled` (`true`/`false`) — включение/отключение перевода текста.
- `ReplaceTextures` (`true`/`false`) — замена текстур (логотип и финал) на русские.

## Благодарности

Deepest Sword создана Cosmic Adventure Squad. Загрузчик — [BepInEx](https://github.com/BepInEx/BepInEx). Перенос ресурсов в AssetBundle и подготовка пакета выполнены с помощью Codex.
