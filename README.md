# QuestionableConverter

The last build shipped under the Dalamud internal name `Questionable`. It exists because
Questionable moved to the internal name `WigglyQuest` (the PunishXIV fork kept `Questionable`,
and Dalamud keys the install folder, the config paths and the update check on that name).

It does no questing. On login it opens a guide that:

1. installs WigglyQuest from the WigglyMuffin plugin repository with one click, and
2. once WigglyQuest is running, points at the plugin installer to remove this plugin.

Its display name is "Questionable (moved to WigglyQuest)" so the installed list tells the two apart:
the new WigglyQuest build keeps the plain "Questionable" name. Display names do not take part in
Dalamud's update or installer matching, which key on the internal name and the repository.

WigglyQuest copies the settings and local quest files on its first start, so the order matters:
install WigglyQuest, let it start, then remove Questionable.
