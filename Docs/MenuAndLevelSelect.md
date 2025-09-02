# Wiring Main Menu & Level Select (Inspector steps)

1) MainMenu scene:
   - Create an empty GameObject `MainMenuUI` and add **MainMenuController**.
   - Buttons:
       - Play Button → OnClick → MainMenuController.GoToLevelSelect
       - How To Play → OnClick → MainMenuController.OpenHowTo
       - Hall of Fame → OnClick → MainMenuController.OpenHallOfFame
       - About → OnClick → MainMenuController.OpenAbout

2) LevelSelect scene:
   - Create an empty GameObject `LevelSelectUI` and add **LevelSelectController**.
   - Buttons:
       - Level 1 → OnClick → LevelSelectController.SelectLevel(1)
       - Level 2 → OnClick → LevelSelectController.SelectLevel(2)
       - Level 3 → OnClick → LevelSelectController.SelectLevel(3)
       - Back   → OnClick → LevelSelectController.BackToMenu()

3) Build Settings:
   - Add scenes in order:
       0: Assets/Scenes/MainMenu.unity
       1: Assets/Scenes/LevelSelect.unity
       2: Assets/Scenes/Grid_Scene.unity
   - Or run **Tools → Scenes → Ensure MainMenu, LevelSelect, Grid_Scene in Build**.

4) Grid_Scene init:
   - In your grid bootstrap (e.g., GridManager.Start), read `GameState.SelectedLevel`.


