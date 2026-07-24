# Free Steam Games — статус сборки

Если сессия Claude оборвалась (кончились токены/закрыт терминал) — начни отсюда.
Полный план: `C:\Users\Red_Dragon\.claude\plans\declarative-gathering-bengio.md`
Задачи отслеживаются в системе задач Claude Code (TaskList) этого проекта — сверить актуальный статус там.

## Сделано
1. ✅ .NET 8 SDK установлен, проект создан в `C:\Users\Red_Dragon\Projects\FreeSteamGames\`, подключены пакеты WPF-UI, H.NotifyIcon.Wpf, CommunityToolkit.Mvvm. (Безобидное предупреждение NU1701 про H.NotifyIcon.Wpf — компилируется нормально.)
2. ✅ `Models/FreeGame.cs`, `Models/SteamFeaturedCategoriesResponse.cs`, `Services/SteamSpecialsService.cs` — запрос к `store.steampowered.com/api/featuredcategories`, фильтр по `discount_percent` (100 = боевой режим, полностью бесплатные раздачи навсегда — НЕ путать с временными "free weekend").
3. ✅ `MainWindow.xaml/.cs`, `ViewModels/MainViewModel.cs` — карточки через WPF-UI (обложка, зачёркнутая цена, бейдж "БЕСПЛАТНО"/FREE, время до конца акции, кнопка "Забрать"/Grab it → браузер на страницу игры).
4. ✅ Трей: `App.xaml.cs` (`SetupTrayIcon`) — иконка `Assets/tray.ico`, меню Открыть/Проверить сейчас/Настройки/Выход, сворачивание вместо закрытия. `Icon="/Assets/tray.ico"` также в MainWindow.xaml (иначе кнопка в панели задач без значка).
5. ✅ `SettingsWindow.xaml/.cs`, `ViewModels/SettingsViewModel.cs`, `Services/SettingsService.cs` (config.json рядом с exe, портативность) — автозапуск, режим запуска (трей/окно), уведомления, интервал проверки.
6. ✅ `Services/AutoStartService.cs` (HKCU...Run) — проверено вживую, галочка пишет/удаляет ключ реестра.
7. ✅ `Services/PollingService.cs` — таймер с самоперепланированием (учитывает изменение интервала на лету), первый прогон сразу, сравнение с `LastSeenAppIds`, toast через `TaskbarIcon.ShowNotification`. **Проверено вживую по-настоящему**: временно занижали порог до 50% и подделывали `LastSeenAppIds`, чтобы искусственно вызвать "новую" раздачу — уведомление Windows реально пришло. Специально не уведомляем на самом первом запуске (когда `LastSeenAppIds` пуст), чтобы не спамить про уже существующие раздачи.
8. ✅ **Локализация (переработана дважды по ходу дела, см. ниже) — готова.**

## Локализация: финальный дизайн (в отличие от первой версии в этом файле ранее!)
Важно: язык интерфейса и валюта — это НЕ одно и то же, разделены на две независимые настройки (первая версия дизайна их путала, пользователь поймал это на живом тесте: физически в Латвии, Windows на русском — валюта должна быть евро независимо от языка текста).

- **Язык интерфейса** (`Models/LocalePack.cs`, `Services/LocalizationService.cs`): JSON-пакет с `code`, `displayName`, `steamLang` (язык названий игр в Steam), `strings` (словарь текстов UI). Встроены `Assets/Locales/ru.json` и `en.json` (копируются в `Locales/` рядом с exe). Скачивание доп. языков — с `https://raw.githubusercontent.com/aivarmobil-oss/FreeSteamGames/main/locales/` (index.json + `{code}.json`) в Настройках, кнопка "Показать ещё языки". Смена языка требует перезапуска (юзер подтверждает диалогом) — `LocalizationService` — `ObservableObject` с индексатором `this[key]`, `Apply()` вызывает `OnPropertyChanged("Item[]")`.
- **Регион/валюта** (`Models/SteamRegion.cs`, `Services/RegionService.cs`): НЕ скачивается — статический список ~13 стран прямо в коде (проверено: `cc=lv` реально даёт EUR через Steam API). Меняется без перезапуска, сразу триггерит `RefreshCommand`.
- **Автоопределение при первом запуске**: язык — из `CultureInfo.CurrentUICulture` (пробуем найти среди установленных, если нет — пробуем скачать с GitHub, иначе fallback на en). Регион — из `RegionInfo.CurrentRegion.TwoLetterISORegionName` (чисто локально, никакой сети/IP-геолокации — решение принято осознанно, чтобы не тревожить антивирус и не задерживать старт).
- **Важный найденный и исправленный баг**: при перезапуске для смены языка приложение всегда открывалось свёрнутым в трей, даже если до перезапуска было открыто окном (потому что просто читало `LaunchMode` из настроек, а не текущее состояние). Исправлено: `RestartApplication()` передаёт `--show-window` в новый процесс, если `_mainWindow.IsVisible` было true; `OnStartup` это учитывает.
- Тексты приложения НЕ локализовывали в UpdateBanner/PollingService notification message — при желании доделать позже (не критично, это редкие технические сообщения).

## Ещё не начато (см. план)
9. GitHub CLI (`winget install GitHub.cli`, `gh auth login` — браузерный OAuth, БЕЗ пароля), репозиторий на аккаунте `aivarmobil-oss`. Нужен НЕ только для version.json (обновления), но и теперь для `locales/index.json` + языковых пакетов — без этого репозитория кнопка "Показать ещё языки" будет падать с ошибкой (это ожидаемо и не баг, просто ещё не сделано).
10. UpdateCheckService + баннер (InfoBar в MainWindow уже забинден на `UpdateBannerVisible`/`UpdateBannerMessage`, самого сервиса чтения version.json ещё нет)
11. `dotnet publish` single-file self-contained win-x64, финальная проверка на чистом запуске (не через `dotnet run`), первый релиз на GitHub

## Важные решения (не переизобретать)
- Стек: WPF (.NET 8) + WPF-UI + H.NotifyIcon.Wpf + CommunityToolkit.Mvvm. Причина отказа от Tauri: на машине нет Rust/MSVC (~5 ГБ ставить), .NET SDK — лёгкая установка.
- Источник данных: официальный (но недокументированный) эндпоинт Steam `featuredcategories`, без ключей и регистрации.
- Валюта = регион Steam (статический список в коде, авто по Windows-региону, ручной override без перезапуска). Язык интерфейса = отдельный скачиваемый JSON-пакет (авто по Windows UI-culture, ручной override с перезапуском).
- Обновление: публичный GitHub-репозиторий `aivarmobil-oss/FreeSteamGames`, `version.json` с min_version, вход только через `gh auth login` (OAuth), пароль никогда не запрашивается и не вводится в чат.
- Настройки хранятся в `config.json` рядом с exe (портативность).
- Название приложения: "Free Steam Games". Анимированная заставка с озвучкой (по типу PlayStation-интро) — в бэклог на будущее, НЕ в v1, пользователь явно согласился отложить.
- Тестовый процесс всегда гоняем через `dotnet run --no-build` в фоне (Bash run_in_background-стиль через `&`), просим пользователя визуально подтвердить в реальном окне — он не может прокручивать терминал (см. ниже), поэтому важные визуальные подтверждения запрашиваем через AskUserQuestion, а не текстом.

## Открытый посторонний баг (не про этот проект)
Прокрутка терминала Claude Code колёсиком мыши не работает — см. memory `project_terminal_scrollback_bug.md`, статус НЕ ИСПРАВЛЕНО, снимается только по явному подтверждению пользователя.
