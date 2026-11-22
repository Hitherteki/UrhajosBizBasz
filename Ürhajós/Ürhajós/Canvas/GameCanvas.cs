using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpaceShooter
{
    public partial class GameCanvas : FrameworkElement
    {
        private const string SettingsPath = "settings.ini";
        private const string SavePath = "savedata.txt";

        private const double BootTypingSpeed = 50;
        private const double BootLineDelay = 0.1;
        private const double BootEmptyLineDelay = 0.05;
        private const double MenuAnimationDuration = 1.1;

        private const double PlayerWidth = 40;
        private const double PlayerHeight = 48;
        private const double ShipDrawWidth = 64;
        private const double ShipDrawHeight = 76.8;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Random _random = new Random();

        private readonly UpgradeSystem _upgrades = new UpgradeSystem();
        private readonly MeteoriteSystem _meteorites = new MeteoriteSystem();
        private readonly BossSystem _bossSystem = new BossSystem();
        private double _shieldRegenerationTimer;
        private double _novaCooldown;

        private readonly GameSettings _settings;
        private WeaponSystem _weapons = new WeaponSystem();
        private CombatFeedback _combat = new CombatFeedback();
        private readonly VisualLayers _visuals = new VisualLayers(3);

        private readonly ObjectPool<Bullet> _bulletPool = new ObjectPool<Bullet>();
        private readonly ObjectPool<EnemyBullet> _enemyBulletPool = new ObjectPool<EnemyBullet>();
        private readonly ObjectPool<Particle> _particlePool = new ObjectPool<Particle>();

        private readonly List<MenuButton> _menuButtons = new List<MenuButton>();
        private readonly List<HighScoreEntry> _highScores = new List<HighScoreEntry>();
        private readonly List<FloatingText> _floatingTexts = new List<FloatingText>();
        private readonly List<Asteroid> _asteroids = new List<Asteroid>();
        private readonly List<Boss> _bosses = new List<Boss>();
        private readonly List<PowerUp> _powerUps = new List<PowerUp>();
        private readonly List<UpgradeOption> _upgradeOptions = new List<UpgradeOption>();

        private readonly Dictionary<Color, SolidColorBrush> _brushes =
            new Dictionary<Color, SolidColorBrush>();

        private readonly Dictionary<string, Pen> _pens =
            new Dictionary<string, Pen>();

        private readonly ImageSource[] _shipImages = new ImageSource[8];

        private readonly Typeface _typeface = new Typeface(
            new FontFamily("Consolas"),
            FontStyles.Normal,
            FontWeights.Bold,
            FontStretches.Normal);

        private readonly string[] _bootLines =
        {
            "[BOOTING NEON VOID OS v2.6]...",
            "[INITIALIZING CORE SYSTEMS]... OK",
            "[CHECKING SHIELD MATRIX]... OK",
            "[CALIBRATING PLASMA CORE]... OK",
            "[LOADING WEAPON MODULES]... OK",
            "[SCANNING ENEMY SIGNATURES]... OK",
            "[SYNCING NAVIGATION ARRAY]... OK",
            "[ESTABLISHING PILOT LINK]... OK",
            "[VERIFYING HULL INTEGRITY]... OK",
            "[CHARGING HYPERDRIVE]... OK",
            "",
            "A JÁTÉKOT KÉSZÍTETTE:",
            "BÁLINT VINCE & REZÁK KEVIN"
        };

        private readonly string[] _menuTexts =
        {
            "JÁTÉK INDÍTÁSA",
            "HANGÁR",
            "RANGLISTA",
            "ÖSSZESÍTÉS",
            "GRAFIKA",
            "KILÉPÉS"
        };

        private readonly MenuAction[] _menuActions =
        {
            MenuAction.Start,
            MenuAction.Hangar,
            MenuAction.HighScores,
            MenuAction.Bestiary,
            MenuAction.Graphics,
            MenuAction.Exit
        };

        private static readonly Color Cyan = Color.FromRgb(0, 230, 255);
        private static readonly Color Gold = Color.FromRgb(255, 205, 80);
        private static readonly Color White = Color.FromRgb(225, 240, 255);
        private static readonly Color Blue = Color.FromRgb(120, 180, 255);
        private static readonly Color Green = Color.FromRgb(60, 235, 140);
        private static readonly Color Red = Color.FromRgb(255, 70, 110);
        private static readonly Color Purple = Color.FromRgb(175, 110, 255);

        private GameState _state = GameState.Boot;

        private int _selectedSkin;
        private int _bootLineIndex;
        private double _bootCharacterProgress;
        private double _bootDelayTimer = -1;
        private bool _bootFinished;

        private double _transitionTimer;
        private double _lastFrameTime;

        private Point _mousePosition;
        private readonly HashSet<Key> _keys = new HashSet<Key>();

        private double _playerX;
        private double _playerY;
        private double _playerHealth = 100;
        private double _playerMaxHealth = 100;
        private double _playerSpeed = 430;
        private double _shield;
        private double _shieldMax = 50;

        private double _dashCooldown;
        private double _dashTime;
        private double _invulnerabilityTime;

        private double _spawnTimer;
        private double _waveTimer;
        private int _wave = 1;
        private int _score;
        private int _level = 1;
        private int _experience;
        private int _experienceNeeded = 100;
        private int _pendingUpgrades;
        private int _bossesDefeated;
        private int _nextBossScore = 3000;

        private double _screenShake;
        private double _flashTime;
        private bool _renderingAttached;

        private int ShipCount
        {
            get { return Math.Min(_shipImages.Length, SkinData.Skins.Length); }
        }

        public GameCanvas()
        {
            Focusable = true;
            ClipToBounds = true;

            _settings = GameSettings.Load(SettingsPath);

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += OnSizeChanged;
            MouseMove += OnMouseMove;
            MouseLeftButtonDown += OnMouseLeftButtonDown;

            LoadShipImages();
            LoadData();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Focus();

            _lastFrameTime = _stopwatch.Elapsed.TotalSeconds;
            _visuals.Initialize(
                Math.Max(1, ActualWidth),
                Math.Max(1, ActualHeight),
                220);

            if (!_renderingAttached)
            {
                CompositionTarget.Rendering += OnRendering;
                _renderingAttached = true;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_renderingAttached)
            {
                CompositionTarget.Rendering -= OnRendering;
                _renderingAttached = false;
            }

            _keys.Clear();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ActualWidth > 0 && ActualHeight > 0)
                _visuals.Initialize(ActualWidth, ActualHeight, 220);
        }

        protected override void OnLostKeyboardFocus(
            KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);

            _keys.Clear();

            if (_state == GameState.Playing)
                _state = GameState.Paused;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            double now = _stopwatch.Elapsed.TotalSeconds;
            double dt = Clamp(now - _lastFrameTime, 0, 0.05);
            _lastFrameTime = now;

            Update(dt);
            InvalidateVisual();
        }

        private void Update(double dt)
        {
            if (ActualWidth <= 0 || ActualHeight <= 0)
                return;

            if (_state != GameState.Paused)
            {
                _visuals.Update(
                    dt,
                    ActualWidth,
                    ActualHeight,
                    0);
            }

            switch (_state)
            {
                case GameState.Boot:
                    UpdateBoot(dt);
                    break;

                case GameState.BootFadeOut:
                    _transitionTimer += dt;

                    if (_transitionTimer >= 0.65)
                    {
                        _transitionTimer = 0;
                        _state = GameState.MenuAwakening;
                    }
                    break;

                case GameState.MenuAwakening:
                    _transitionTimer += dt;

                    if (_transitionTimer >= MenuAnimationDuration)
                        FinishMenuAnimation();
                    break;

                case GameState.Playing:
                    UpdateGame(dt);
                    break;
            }

            if (_state != GameState.Paused &&
                _state != GameState.Upgrade)
            {
                UpdateEffects(dt);
            }
        }

        private void UpdateBoot(double dt)
        {
            if (_bootFinished)
                return;

            if (_bootLineIndex >= _bootLines.Length)
            {
                _bootFinished = true;
                return;
            }

            string line = _bootLines[_bootLineIndex];

            if (line.Length > 0 &&
                _bootCharacterProgress < line.Length)
            {
                _bootCharacterProgress = Math.Min(
                    line.Length,
                    _bootCharacterProgress + dt * BootTypingSpeed);

                return;
            }

            if (_bootDelayTimer < 0)
            {
                _bootDelayTimer = line.Length == 0
                    ? BootEmptyLineDelay
                    : BootLineDelay;
            }

            _bootDelayTimer -= dt;

            if (_bootDelayTimer <= 0)
            {
                _bootLineIndex++;
                _bootCharacterProgress = 0;
                _bootDelayTimer = -1;

                if (_bootLineIndex >= _bootLines.Length)
                    _bootFinished = true;
            }
        }

        private void BeginBootFadeOut()
        {
            if (_state != GameState.Boot || !_bootFinished)
                return;

            _keys.Clear();
            _transitionTimer = 0;
            _state = GameState.BootFadeOut;
        }

        private void FinishMenuAnimation()
        {
            _transitionTimer = MenuAnimationDuration;
            _screenShake = 0;
            _state = GameState.Menu;
            _menuButtons.Clear();
        }

        private void UpdateGame(double dt)
        {
            _combat.Update(dt);
            _novaCooldown = Math.Max(0, _novaCooldown - dt);

            UpdatePlayer(dt);
            UpdateAsteroids(dt);

            if (_state != GameState.Playing)
                return;

            UpdateBosses(dt);
            UpdateBullets(dt);

            if (_state != GameState.Playing)
                return;

            UpdateEnemyBullets(dt);

            if (_state != GameState.Playing)
                return;

            UpdatePowerUps(dt);
            UpdateSpawning(dt);
        }

        private void UpdatePlayer(double dt)
        {
            double dx = 0;
            double dy = 0;

            if (IsPressed(Key.A, Key.Left))
                dx--;

            if (IsPressed(Key.D, Key.Right))
                dx++;

            if (IsPressed(Key.W, Key.Up))
                dy--;

            if (IsPressed(Key.S, Key.Down))
                dy++;

            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length > 0)
            {
                dx /= length;
                dy /= length;
            }

            double speed = _playerSpeed *
                SkinData.Skins[_selectedSkin].SpeedMultiplier;

            if (_dashTime > 0)
                speed *= 2.7;

            _dashTime = Math.Max(0, _dashTime - dt);
            _dashCooldown = Math.Max(0, _dashCooldown - dt);
            _invulnerabilityTime = Math.Max(
                0,
                _invulnerabilityTime - dt);

            _playerX += dx * speed * dt;
            _playerY += dy * speed * dt;

            double minY = ActualHeight * 0.42;

            _playerX = Clamp(
                _playerX,
                18,
                Math.Max(18, ActualWidth - PlayerWidth - 18));

            _playerY = Clamp(
                _playerY,
                minY,
                Math.Max(minY, ActualHeight - PlayerHeight - 65));

            _weapons.Update(dt);

            if (_keys.Contains(Key.Space))
            {
                List<Bullet> bullets =
                    (List<Bullet>)_bulletPool.Active;

                if (_weapons.TryShoot(
                    bullets,
                    _playerX,
                    _playerY,
                    PlayerWidth))
                {
                    SpawnThrusterParticle();
                }
            }
        }

        private bool IsPressed(Key first, Key second)
        {
            return _keys.Contains(first) || _keys.Contains(second);
        }

        private void UpdateBullets(double dt)
        {
            IReadOnlyList<Bullet> bullets = _bulletPool.Active;

            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                Bullet bullet = bullets[i];

                bullet.PreviousX = bullet.X;
                bullet.PreviousY = bullet.Y;
                bullet.X += bullet.Vx * dt;
                bullet.Y -= bullet.Speed * dt;

                bool hit = false;

                for (int j = _asteroids.Count - 1; j >= 0; j--)
                {
                    Asteroid asteroid = _asteroids[j];

                    double radius = asteroid.Radius +
                        Math.Max(bullet.Width, bullet.Height) * 0.5;

                    if (!SegmentHitsCircle(
                        bullet.PreviousX + bullet.Width / 2,
                        bullet.PreviousY + bullet.Height / 2,
                        bullet.X + bullet.Width / 2,
                        bullet.Y + bullet.Height / 2,
                        asteroid.X,
                        asteroid.Y,
                        radius))
                    {
                        continue;
                    }

                    asteroid.Health -= bullet.Damage;

                    SpawnExplosion(
                        bullet.X,
                        bullet.Y,
                        WeaponSystem.GetBulletColor(bullet),
                        6);

                    if (asteroid.Health <= 0)
                        DestroyAsteroid(j);

                    hit = true;
                    break;
                }

                if (!hit)
                {
                    for (int j = _bosses.Count - 1; j >= 0; j--)
                    {
                        Boss boss = _bosses[j];

                        Rect swept = new Rect(
                            Math.Min(bullet.PreviousX, bullet.X),
                            Math.Min(bullet.PreviousY, bullet.Y),
                            Math.Abs(bullet.X - bullet.PreviousX) +
                                bullet.Width,
                            Math.Abs(bullet.Y - bullet.PreviousY) +
                                bullet.Height);

                        Rect target = new Rect(
                            boss.X,
                            boss.Y,
                            boss.Width,
                            boss.Height);

                        if (!swept.IntersectsWith(target))
                            continue;

                        boss.Health -= bullet.Damage;

                        SpawnExplosion(
                            bullet.X,
                            bullet.Y,
                            WeaponSystem.GetBulletColor(bullet),
                            6);

                        if (boss.Health <= 0)
                        {
                            _bosses.RemoveAt(j);
                            DefeatBoss(boss);
                        }

                        hit = true;
                        break;
                    }
                }

                if (hit ||
                    bullet.Y < -80 ||
                    bullet.X < -80 ||
                    bullet.X > ActualWidth + 80)
                {
                    _bulletPool.Release(i);
                }

                if (_state != GameState.Playing)
                    return;
            }
        }

        private static bool SegmentHitsCircle(
            double x1,
            double y1,
            double x2,
            double y2,
            double cx,
            double cy,
            double radius)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double lengthSquared = dx * dx + dy * dy;
            double t = 0;

            if (lengthSquared > 0)
            {
                t = Clamp(
                    ((cx - x1) * dx + (cy - y1) * dy) /
                        lengthSquared,
                    0,
                    1);
            }

            double distanceX = x1 + dx * t - cx;
            double distanceY = y1 + dy * t - cy;

            return distanceX * distanceX +
                distanceY * distanceY <= radius * radius;
        }

        private void DestroyAsteroid(int index)
        {
            Asteroid asteroid = _asteroids[index];
            _asteroids.RemoveAt(index);

            int baseScore = asteroid.Type == 0
                ? 50
                : asteroid.Type == 1 ? 120 : 220;

            _combat.RegisterKill();
            _score += _combat.ApplyScore(baseScore);

            SpawnExplosion(
                asteroid.X,
                asteroid.Y,
                Purple,
                26);

            if (_random.NextDouble() < 0.08)
            {
                _powerUps.Add(new PowerUp
                {
                    X = asteroid.X,
                    Y = asteroid.Y,
                    Type = _random.Next(7),
                    Rotation = 0
                });
            }

            AddExperience(12);
        }

        private void UpdateAsteroids(double dt)
        {
            for (int i = _asteroids.Count - 1; i >= 0; i--)
            {
                Asteroid asteroid = _asteroids[i];

                asteroid.Y += asteroid.Speed * dt;
                asteroid.Rotation += asteroid.RotationSpeed * dt;

                if (asteroid.Y - asteroid.Radius > ActualHeight)
                {
                    _asteroids.RemoveAt(i);
                    continue;
                }

                if (!CheckPlayerCollision(
                    asteroid.X,
                    asteroid.Y,
                    asteroid.Radius))
                {
                    continue;
                }

                _asteroids.RemoveAt(i);

                SpawnExplosion(
                    asteroid.X,
                    asteroid.Y,
                    Red,
                    22);

                DamagePlayer(
                    asteroid.Type == 0
                        ? 10
                        : asteroid.Type == 1 ? 16 : 22);

                if (_state != GameState.Playing)
                    return;
            }
        }

        private void UpdateBosses(double dt)
        {
            foreach (Boss boss in _bosses)
            {
                boss.Phase.Update(boss, dt);
                boss.X += boss.Speed * boss.Direction * dt;

                if (boss.X <= 30)
                {
                    boss.X = 30;
                    boss.Direction = 1;
                }
                else if (boss.X >= ActualWidth - boss.Width - 30)
                {
                    boss.X = ActualWidth - boss.Width - 30;
                    boss.Direction = -1;
                }

                boss.ShootTimer -= dt;

                if (boss.ShootTimer <= 0)
                {
                    ShootBoss(boss);

                    boss.ShootTimer = boss.Phase.FireInterval(
                        BossData.GetFireInterval(_bossesDefeated));
                }
            }
        }

        private void UpdateEnemyBullets(double dt)
        {
            IReadOnlyList<EnemyBullet> bullets =
                _enemyBulletPool.Active;

            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                EnemyBullet bullet = bullets[i];

                bullet.X += bullet.Vx * dt;
                bullet.Y += bullet.Vy * dt;

                if (bullet.Y > ActualHeight + 30 ||
                    bullet.Y < -100 ||
                    bullet.X < -30 ||
                    bullet.X > ActualWidth + 30)
                {
                    _enemyBulletPool.Release(i);
                    continue;
                }

                if (CheckPlayerCollision(
                    bullet.X,
                    bullet.Y,
                    bullet.Radius))
                {
                    _enemyBulletPool.Release(i);
                    DamagePlayer(8);

                    if (_state != GameState.Playing)
                        return;
                }
            }
        }

        private void UpdatePowerUps(double dt)
        {
            for (int i = _powerUps.Count - 1; i >= 0; i--)
            {
                PowerUp powerUp = _powerUps[i];
                powerUp.Y += 90 * dt;
                powerUp.Rotation += dt * 3;

                if (powerUp.Y > ActualHeight + 30)
                {
                    _powerUps.RemoveAt(i);
                    continue;
                }

                if (CheckPlayerCollision(powerUp.X, powerUp.Y, 18))
                {
                    ApplyPowerUp(powerUp.Type);
                    _powerUps.RemoveAt(i);
                }
            }
        }

        private void UpdateSpawning(double dt)
        {
            _waveTimer += dt;

            if (_waveTimer >= 20)
            {
                _waveTimer -= 20;
                _wave++;
            }

            _spawnTimer -= dt;

            if (_spawnTimer > 0)
                return;

            if (_bosses.Count == 0 && _score >= _nextBossScore)
            {
                SpawnBoss();
                _spawnTimer = 3;
                return;
            }

            if (_asteroids.Count < 60)
                SpawnAsteroid();

            double difficulty = 1 + (_wave - 1) * 0.12;
            _spawnTimer = Math.Max(0.2, 0.82 / difficulty);
        }

        private void SpawnAsteroid()
        {
            double radius = _random.Next(16, 38);
            int type = 0;

            if (_wave >= 3 && _random.NextDouble() < 0.24)
                type = 1;

            if (_wave >= 6 && _random.NextDouble() < 0.14)
                type = 2;

            double health = type == 0 ? 1 : type == 1 ? 3 : 6;
            double difficulty = 1 + (_wave - 1) * 0.12;
            int vertices = _random.Next(7, 11);
            double[] offsets = new double[vertices];

            for (int i = 0; i < offsets.Length; i++)
                offsets[i] = 0.8 + _random.NextDouble() * 0.35;

            _asteroids.Add(new Asteroid
            {
                X = radius + _random.NextDouble() *
                    Math.Max(1, ActualWidth - radius * 2),
                Y = -radius - 20,
                Radius = radius,
                Speed = _random.Next(135, 275) *
                    difficulty * (type == 0 ? 1 : type == 1 ? 0.78 : 0.62),
                Health = health,
                MaxHealth = health,
                Type = type,
                Rotation = _random.NextDouble() * Math.PI * 2,
                RotationSpeed = (_random.NextDouble() - 0.5) * 3.4,
                RadiusOffsets = offsets
            });
        }

        private void SpawnBoss()
        {
            double width = Math.Min(280, ActualWidth * 0.32);
            double health = BossData.GetHealth(_bossesDefeated);

            _bosses.Add(new Boss
            {
                Kind = (BossKind)(_bossesDefeated % 5),
                X = ActualWidth / 2 - width / 2,
                Y = 60,
                Width = width,
                Height = width * 0.55,
                Health = health,
                MaxHealth = health,
                Speed = BossData.GetSpeed(_bossesDefeated),
                ShootTimer = 1,
                Direction = 1
            });
        }

        private void ShootBoss(Boss boss)
        {
            int count = boss.Phase.ProjectileMultiplier;

            for (int i = 0; i < count; i++)
            {
                double angle = Math.PI / 2 +
                    (i - (count - 1) / 2.0) * 0.22;

                EnemyBullet bullet = _enemyBulletPool.Get();

                bullet.X = boss.X + boss.Width / 2;
                bullet.Y = boss.Y + boss.Height;
                bullet.Vx = Math.Cos(angle) * 260;
                bullet.Vy = Math.Sin(angle) * 260;
                bullet.Radius = 7;
            }
        }

        private void DefeatBoss(Boss boss)
        {
            _bossesDefeated++;
            _score += 1500;
            _nextBossScore += 3000;

            SpawnExplosion(
                boss.X + boss.Width / 2,
                boss.Y + boss.Height / 2,
                BossData.Colors[(int)boss.Kind],
                70);

            AddExperience(180);
        }

        private void ApplyPowerUp(int type)
        {
            string text;
            Color color;

            switch (type)
            {
                case 0:
                    _playerHealth = Math.Min(
                        _playerMaxHealth,
                        _playerHealth + 25);
                    text = "+25 HP";
                    color = Green;
                    break;

                case 1:
                    _shield = _shieldMax;
                    text = "PAJZS";
                    color = Cyan;
                    break;

                case 2:
                    _weapons.SetWeapon(BulletType.Rocket);
                    text = "RAKÉTA";
                    color = Gold;
                    break;

                case 3:
                    _weapons.SetWeapon(BulletType.Laser);
                    text = "LÉZER";
                    color = Green;
                    break;

                case 4:
                    _weapons.SetWeapon(BulletType.Shotgun);
                    text = "SÖRÉT";
                    color = Gold;
                    break;

                case 5:
                    _weapons.SetWeapon(BulletType.Charged);
                    text = "TÖLTÖTT FEGYVER";
                    color = Purple;
                    break;

                default:
                    _novaCooldown = 0;
                    text = "NOVA KÉSZ";
                    color = Gold;
                    break;
            }

            _floatingTexts.Add(new FloatingText
            {
                Text = text,
                X = _playerX + PlayerWidth / 2,
                Y = _playerY,
                Life = 1.2,
                MaxLife = 1.2,
                Color = color
            });
        }

        private void DamagePlayer(double damage)
        {
            if (_state != GameState.Playing ||
                _invulnerabilityTime > 0)
            {
                return;
            }

            double absorbed = Math.Min(_shield, damage);
            _shield -= absorbed;
            _playerHealth -= damage - absorbed;

            _invulnerabilityTime = 0.35;

            if (_settings.ScreenShake)
                _screenShake = 8;

            if (_settings.ScreenFlash)
                _flashTime = 0.2;

            if (_playerHealth <= 0)
            {
                _playerHealth = 0;
                SaveData(true);
                ReturnToMenu();
            }
        }

        private void StartGame()
        {
            if (ShipCount == 0)
                return;

            ClearWorld();
            _keys.Clear();

            ShipSkin skin = SkinData.Skins[_selectedSkin];

            _weapons = new WeaponSystem();
            _weapons.ApplySkin(skin);
            _weapons.SetWeapon(BulletType.Plasma);
            _combat = new CombatFeedback();

            _playerMaxHealth = 100 + skin.HealthBonus;
            _playerHealth = _playerMaxHealth;
            _playerSpeed = 430;
            _shield = 0;
            _shieldMax = 50;

            _playerX = ActualWidth / 2 - PlayerWidth / 2;
            _playerY = ActualHeight - 160;

            _dashCooldown = 0;
            _dashTime = 0;
            _invulnerabilityTime = 0;
            _novaCooldown = 0;

            _spawnTimer = 0.5;
            _waveTimer = 0;
            _wave = 1;
            _score = 0;
            _level = 1;
            _experience = 0;
            _experienceNeeded = 100;
            _pendingUpgrades = 0;
            _bossesDefeated = 0;
            _nextBossScore = 3000;
            _screenShake = 0;
            _flashTime = 0;

            _state = GameState.Playing;
        }

        private void ClearWorld()
        {
            _asteroids.Clear();
            _bosses.Clear();
            _powerUps.Clear();
            _floatingTexts.Clear();
            _upgradeOptions.Clear();
            _bulletPool.Clear();
            _enemyBulletPool.Clear();
            _particlePool.Clear();
            _menuButtons.Clear();
        }

        private void ReturnToMenu()
        {
            ClearWorld();
            _keys.Clear();
            _screenShake = 0;
            _flashTime = 0;
            _state = GameState.Menu;
        }

        private void AddExperience(int amount)
        {
            _experience += amount;

            while (_experience >= _experienceNeeded)
            {
                _experience -= _experienceNeeded;
                _level++;
                _experienceNeeded = (int)(_experienceNeeded * 1.25);
                _pendingUpgrades++;
            }

            if (_pendingUpgrades > 0)
            {
                _keys.Clear();
                GenerateUpgrades();
                _state = GameState.Upgrade;
            }
        }

        private void GenerateUpgrades()
        {
            _upgradeOptions.Clear();
            _upgradeOptions.AddRange(_upgrades.Generate(_weapons.Weapon));
        }

        private static string GetUpgradeTitle(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Damage: return "SEBZÉS";
                case UpgradeType.FireRate: return "TŰZGYORSASÁG";
                case UpgradeType.Speed: return "HAJTÓMŰ";
                case UpgradeType.Health: return "PÁNCÉLZAT";
                case UpgradeType.Shield: return "PAJZS";
                case UpgradeType.Multishot: return "TÖBBSZÖRÖS LÖVÉS";
                case UpgradeType.BulletSpeed: return "LÖVEDÉKSEBESSÉG";
                case UpgradeType.HeatCapacity: return "HŐKAPACITÁS";
                case UpgradeType.Cooling: return "HŰTÉS";
                default: return "HATÉKONYSÁG";
            }
        }

        private static string GetUpgradeDescription(UpgradeType type)
        {
            switch (type)
            {
                case UpgradeType.Damage: return "Nagyobb sebzés";
                case UpgradeType.FireRate: return "Gyorsabb tüzelés";
                case UpgradeType.Speed: return "Gyorsabb hajó";
                case UpgradeType.Health: return "Több életerő";
                case UpgradeType.Shield: return "Erősebb pajzs";
                case UpgradeType.Multishot: return "Több plazmalövedék";
                case UpgradeType.BulletSpeed: return "Gyorsabb lövedékek";
                case UpgradeType.HeatCapacity: return "Nagyobb hőtűrés";
                case UpgradeType.Cooling: return "Gyorsabb hűlés";
                default: return "Hatékonyabb fegyver";
            }
        }

        private void SelectUpgradeByIndex(int index)
        {
            if (_state != GameState.Upgrade || index < 0 || index >= _upgradeOptions.Count)
                return;

            UpgradeOption option = _upgradeOptions[index];

            switch (option.Type)
            {
                case UpgradeType.Health:
                    _playerMaxHealth += 20;
                    _playerHealth = _playerMaxHealth;
                    break;

                case UpgradeType.Shield:
                    _shieldMax += 15;
                    _shield = _shieldMax;
                    break;

                case UpgradeType.Speed:
                    _playerSpeed *= 1.08;
                    break;

                default:
                    _upgrades.Apply(option.Type, _weapons);
                    break;
            }

            _pendingUpgrades = Math.Max(0, _pendingUpgrades - 1);
            _menuButtons.Clear();

            if (_pendingUpgrades > 0)
                GenerateUpgrades();
            else
                _state = GameState.Playing;
        }

        private bool CheckPlayerCollision(
            double x,
            double y,
            double radius)
        {
            double dx = x - (_playerX + PlayerWidth / 2);
            double dy = y - (_playerY + PlayerHeight / 2);
            double combined = radius + 18;

            return dx * dx + dy * dy < combined * combined;
        }

        private void ActivateNova()
        {
            if (_novaCooldown > 0)
                return;

            _novaCooldown = 12;
            _invulnerabilityTime = Math.Max(
                _invulnerabilityTime,
                0.8);

            _enemyBulletPool.Clear();

            for (int i = _asteroids.Count - 1; i >= 0; i--)
            {
                Asteroid asteroid = _asteroids[i];

                SpawnExplosion(
                    asteroid.X,
                    asteroid.Y,
                    Cyan,
                    12);

                _score += 50;
            }

            _asteroids.Clear();

            SpawnExplosion(
                _playerX + PlayerWidth / 2,
                _playerY + PlayerHeight / 2,
                Cyan,
                70);
        }

        private void UpdateEffects(double dt)
        {
            IReadOnlyList<Particle> particles = _particlePool.Active;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle particle = particles[i];
                particle.X += particle.Vx * dt;
                particle.Y += particle.Vy * dt;
                particle.Life -= dt;

                if (particle.Life <= 0)
                    _particlePool.Release(i);
            }

            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                FloatingText text = _floatingTexts[i];
                text.Y -= 34 * dt;
                text.Life -= dt;

                if (text.Life <= 0)
                    _floatingTexts.RemoveAt(i);
            }

            _screenShake = Math.Max(0, _screenShake - dt * 28);
            _flashTime = Math.Max(0, _flashTime - dt);
        }

        private void SpawnExplosion(
            double x,
            double y,
            Color color,
            int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_particlePool.Active.Count >= _settings.ParticleLimit)
                    break;

                double angle = _random.NextDouble() * Math.PI * 2;
                double speed = 80 + _random.NextDouble() * 260;

                Particle particle = _particlePool.Get();

                particle.X = x;
                particle.Y = y;
                particle.Vx = Math.Cos(angle) * speed;
                particle.Vy = Math.Sin(angle) * speed;
                particle.Life = 0.35 + _random.NextDouble() * 0.45;
                particle.MaxLife = particle.Life;
                particle.Size = 2 + _random.NextDouble() * 3;
                particle.Color = color;
            }
        }

        private void SpawnThrusterParticle()
        {
            if (_particlePool.Active.Count >= _settings.ParticleLimit)
                return;

            Particle particle = _particlePool.Get();

            particle.X = _playerX + PlayerWidth / 2;
            particle.Y = _playerY + PlayerHeight;
            particle.Vx = (_random.NextDouble() - 0.5) * 60;
            particle.Vy = 180 + _random.NextDouble() * 160;
            particle.Life = 0.3;
            particle.MaxLife = 0.3;
            particle.Size = 3;
            particle.Color = SkinData.Skins[_selectedSkin].Engine;
        }

        private void LoadShipImages()
        {
            for (int i = 0; i < _shipImages.Length; i++)
            {
                try
                {
                    BitmapImage image = new BitmapImage();

                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(
                        "pack://application:,,,/Assets/Ships/ship" +
                        (i + 1) + ".png",
                        UriKind.Absolute);
                    image.EndInit();
                    image.Freeze();

                    _shipImages[i] = image;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(
                        "Hajókép betöltési hiba: " + ex.Message);

                    _shipImages[i] = null;
                }
            }
        }

        private void LoadData()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return;

                foreach (string line in File.ReadAllLines(SavePath))
                {
                    string[] parts = line.Split('|');

                    int score;
                    int level;
                    int bosses;
                    DateTime date;

                    if (parts.Length != 4 ||
                        !int.TryParse(parts[0], out score) ||
                        !int.TryParse(parts[1], out level) ||
                        !int.TryParse(parts[2], out bosses) ||
                        !DateTime.TryParse(
                            parts[3],
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out date))
                    {
                        continue;
                    }

                    _highScores.Add(new HighScoreEntry
                    {
                        Score = score,
                        Level = level,
                        Bosses = bosses,
                        Date = date
                    });
                }

                _highScores.Sort(
                    (a, b) => b.Score.CompareTo(a.Score));

                if (_highScores.Count > 10)
                    _highScores.RemoveRange(10, _highScores.Count - 10);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void SaveData(bool addCurrentScore)
        {
            if (addCurrentScore)
            {
                _highScores.Add(new HighScoreEntry
                {
                    Score = _score,
                    Level = _level,
                    Bosses = _bossesDefeated,
                    Date = DateTime.Now
                });
            }

            _highScores.Sort((a, b) => b.Score.CompareTo(a.Score));

            if (_highScores.Count > 10)
                _highScores.RemoveRange(10, _highScores.Count - 10);

            try
            {
                List<string> lines = new List<string>();

                foreach (HighScoreEntry entry in _highScores)
                {
                    lines.Add(string.Join(
                        "|",
                        entry.Score.ToString(CultureInfo.InvariantCulture),
                        entry.Level.ToString(CultureInfo.InvariantCulture),
                        entry.Bosses.ToString(CultureInfo.InvariantCulture),
                        entry.Date.ToString(
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture)));
                }

                File.WriteAllLines(SavePath, lines);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private int GetHighScore()
        {
            return _highScores.Count == 0
                ? 0
                : _highScores[0].Score;
        }

        private void ChangeSkin(int direction)
        {
            if (ShipCount == 0)
                return;

            _selectedSkin =
                (_selectedSkin + direction + ShipCount) % ShipCount;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (ActualWidth <= 0 || ActualHeight <= 0)
                return;

            _menuButtons.Clear();

            dc.DrawRectangle(
                BrushFor(Color.FromRgb(5, 7, 16)),
                null,
                new Rect(0, 0, ActualWidth, ActualHeight));

            _visuals.Render(
                dc,
                ActualWidth,
                ActualHeight,
                _settings.GlowEffects);

            if (_state == GameState.Boot ||
                _state == GameState.BootFadeOut)
            {
                DrawBootScreen(dc);
                return;
            }

            switch (_state)
            {
                case GameState.MenuAwakening:
                    DrawMenu(
                        dc,
                        Clamp(
                            _transitionTimer / MenuAnimationDuration,
                            0,
                            1));
                    break;

                case GameState.Menu:
                    DrawMenu(dc, 1);
                    break;

                case GameState.Hangar:
                    DrawHangar(dc);
                    break;

                case GameState.HighScores:
                    DrawHighScores(dc);
                    break;

                case GameState.Bestiary:
                    DrawBestiary(dc);
                    break;

                case GameState.Graphics:
                    DrawGraphicsMenu(dc);
                    break;

                case GameState.Playing:
                case GameState.Paused:
                case GameState.Upgrade:
                    DrawGame(dc);

                    if (_state == GameState.Paused)
                        DrawPauseMenu(dc);
                    else if (_state == GameState.Upgrade)
                        DrawUpgradeMenu(dc);
                    break;
            }
        }

        private void DrawBootScreen(DrawingContext dc)
        {
            dc.DrawRectangle(
                Brushes.Black,
                null,
                new Rect(0, 0, ActualWidth, ActualHeight));

            double opacity = _state == GameState.BootFadeOut
                ? 1 - Clamp(_transitionTimer / 0.5, 0, 1)
                : 1;

            double spacing = Math.Min(
                34,
                Math.Max(16, (ActualHeight - 180) / _bootLines.Length));

            double fontSize = Math.Min(20, spacing * 0.7);
            double x = Math.Min(60, ActualWidth * 0.05);
            double y = Math.Min(70, ActualHeight * 0.08);

            dc.PushOpacity(opacity);

            for (int i = 0; i < _bootLines.Length; i++)
            {
                if (i > _bootLineIndex)
                    break;

                string text = _bootLines[i];

                if (i == _bootLineIndex && !_bootFinished)
                {
                    int count = Math.Min(
                        text.Length,
                        (int)_bootCharacterProgress);

                    text = text.Substring(0, count);
                }

                DrawText(
                    dc,
                    text,
                    fontSize,
                    i >= 11 ? Cyan : Green,
                    x,
                    y + i * spacing);
            }

            if (_bootFinished)
            {
                dc.PushOpacity(
                    0.65 +
                    Math.Sin(_stopwatch.Elapsed.TotalSeconds * 4) * 0.25);

                DrawCentered(
                    dc,
                    "[ NYOMJ LE EGY GOMBOT A FOLYTATÁSHOZ ]",
                    20,
                    Green,
                    ActualWidth / 2,
                    ActualHeight - 85,
                    ActualWidth - 40);

                dc.Pop();
            }

            dc.Pop();
        }

        private void GetMenuPanels(
            out Rect left,
            out Rect right)
        {
            double margin = ActualWidth * 0.06;
            double gap = ActualWidth * 0.055;
            double available = Math.Max(20, ActualWidth - margin * 2 - gap);

            double leftWidth = available * 0.46;
            double rightWidth = available - leftWidth;
            double height = ActualHeight * 0.76;
            double top = (ActualHeight - height) / 2;

            left = new Rect(
                margin,
                top,
                leftWidth,
                height);

            right = new Rect(
                margin + leftWidth + gap,
                top,
                rightWidth,
                height);
        }

        private void DrawMenu(DrawingContext dc, double progress)
        {
            Rect left;
            Rect right;
            GetMenuPanels(out left, out right);

            double reveal = EaseOut(progress);
            double slide = (1 - reveal) * ActualWidth * 0.07;

            left.Offset(-slide, 0);
            right.Offset(slide, 0);

            DrawCornerMarkers(dc, reveal);

            dc.PushOpacity(reveal);

            DrawPanel(dc, left);
            DrawPanel(dc, right);

            Geometry leftClip = TechGeometry(left, 28);
            dc.PushClip(leftClip);

            double padding = Math.Min(36, left.Width * 0.09);
            double titleSize = Math.Min(56, left.Width * 0.12);
            double titleY = left.Y + left.Height * 0.065;

            DrawTextFitted(
                dc,
                RevealText(
                    "NEON VOID",
                    Clamp(progress / 0.25, 0, 1)),
                titleSize,
                Gold,
                new Rect(
                    left.X + padding,
                    titleY,
                    Math.Max(1, left.Width - padding * 2),
                    left.Height * 0.1),
                false);

            double dividerY = left.Y + left.Height * 0.19;
            double dividerWidth = left.Width - padding * 2;

            dc.DrawLine(
                PenFor(Cyan, 2),
                new Point(left.X + padding, dividerY),
                new Point(
                    left.X + padding + dividerWidth * 0.55,
                    dividerY));

            dc.DrawLine(
                PenFor(Gold, 2),
                new Point(
                    left.X + padding + dividerWidth * 0.55,
                    dividerY),
                new Point(left.Right - padding, dividerY));

            double startY = left.Y + left.Height * 0.25;
            double bottomPadding = left.Height * 0.075;
            double availableHeight =
                left.Bottom - bottomPadding - startY;

            double gap = Math.Min(15, availableHeight * 0.035);
            double buttonHeight =
                (availableHeight - gap * (_menuTexts.Length - 1)) /
                _menuTexts.Length;

            double buttonWidth = Math.Max(
                1,
                left.Width - padding * 2 - 10);

            for (int i = 0; i < _menuTexts.Length; i++)
            {
                double delay = i * 0.075;
                double local = Clamp(
                    (progress - delay) / (1 - delay),
                    0,
                    1);

                double eased = EaseOut(local);
                double x = left.X + padding +
                    (1 - eased) * 22;

                Rect baseBounds = new Rect(
                    x,
                    startY + i * (buttonHeight + gap),
                    buttonWidth,
                    Math.Max(1, buttonHeight));

                bool hovered =
                    _state == GameState.Menu &&
                    baseBounds.Contains(_mousePosition);

                Rect bounds = baseBounds;

                if (hovered)
                    bounds.Offset(8, 0);

                dc.PushOpacity(eased);

                DrawTechButton(
                    dc,
                    bounds,
                    RevealText(
                        _menuTexts[i],
                        Clamp((progress - delay) / 0.23, 0, 1)),
                    hovered);

                dc.Pop();

                if (_state == GameState.Menu)
                {
                    Rect hitBounds = Rect.Union(baseBounds, bounds);

                    AddMenuButton(
                        _menuActions[i],
                        hitBounds,
                        _menuTexts[i]);
                }
            }

            dc.Pop();

            dc.PushClip(TechGeometry(right, 28));
            DrawShipShowcase(dc, right, progress);
            dc.Pop();

            dc.Pop();
        }

        private void DrawShipShowcase(
            DrawingContext dc,
            Rect panel,
            double progress)
        {
            double time = _stopwatch.Elapsed.TotalSeconds;
            double centerX = panel.X + panel.Width / 2;

            double imageAreaTop = panel.Y + panel.Height * 0.06;
            double imageAreaHeight = panel.Height * 0.53;
            double centerY = imageAreaTop + imageAreaHeight / 2;

            double radius = Math.Min(
                panel.Width * 0.29,
                imageAreaHeight * 0.43);

            DrawRadar(dc, centerX, centerY, radius, time);

            ImageSource image = _shipImages[_selectedSkin];

            if (image != null)
            {
                Rect imageArea = new Rect(
                    panel.X + panel.Width * 0.15,
                    imageAreaTop + 12 +
                        Math.Sin(time * 2.5) * 8,
                    panel.Width * 0.7,
                    Math.Max(1, imageAreaHeight - 24));

                dc.DrawImage(image, FitImage(image, imageArea));
            }

            double infoTop = panel.Y + panel.Height * 0.64;
            double lineHeight = panel.Height * 0.068;
            double maxWidth = Math.Max(1, panel.Width - 48);

            dc.PushOpacity(
                Clamp((progress - 0.62) / 0.38, 0, 1));

            DrawCentered(
                dc,
                SkinData.Skins[_selectedSkin].Name,
                Math.Min(26, panel.Width * 0.055),
                Gold,
                centerX,
                infoTop,
                maxWidth);

            DrawCentered(
                dc,
                "REKORD: " + GetHighScore(),
                Math.Min(20, panel.Width * 0.043),
                White,
                centerX,
                infoTop + lineHeight,
                maxWidth);

            DrawCentered(
                dc,
                "PROFIL: " +
                    (_selectedSkin + 1).ToString("00") +
                    " / " + ShipCount.ToString("00"),
                Math.Min(17, panel.Width * 0.037),
                Blue,
                centerX,
                infoTop + lineHeight * 2,
                maxWidth);

            DrawCentered(
                dc,
                "NEON VOID // v2.6",
                Math.Min(17, panel.Width * 0.037),
                Blue,
                centerX,
                infoTop + lineHeight * 3,
                maxWidth);

            dc.Pop();
        }

        private void DrawRadar(
            DrawingContext dc,
            double x,
            double y,
            double radius,
            double time)
        {
            Color faint = Color.FromArgb(55, 0, 230, 255);
            Point center = new Point(x, y);

            dc.DrawEllipse(
                null,
                PenFor(faint, 1.5),
                center,
                radius,
                radius);

            dc.DrawEllipse(
                null,
                PenFor(faint, 1),
                center,
                radius * 0.66,
                radius * 0.66);

            dc.DrawLine(
                PenFor(faint, 1),
                new Point(x - radius, y),
                new Point(x + radius, y));

            dc.DrawLine(
                PenFor(faint, 1),
                new Point(x, y - radius),
                new Point(x, y + radius));

            dc.DrawLine(
                PenFor(Color.FromArgb(90, 0, 230, 255), 1.5),
                center,
                new Point(
                    x + Math.Cos(time * 0.6) * radius,
                    y + Math.Sin(time * 0.6) * radius));
        }

        private void DrawPanel(DrawingContext dc, Rect bounds)
        {
            dc.DrawGeometry(
                BrushFor(Color.FromArgb(155, 6, 10, 24)),
                PenFor(Cyan, 1.5),
                TechGeometry(bounds, 28));
        }

        private static Geometry TechGeometry(Rect bounds, double cut)
        {
            cut = Math.Min(
                cut,
                Math.Min(bounds.Width, bounds.Height) * 0.25);

            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(
                    new Point(bounds.X + cut, bounds.Y),
                    true,
                    true);

                context.LineTo(
                    new Point(bounds.Right, bounds.Y),
                    true,
                    false);

                context.LineTo(
                    new Point(bounds.Right, bounds.Bottom - cut),
                    true,
                    false);

                context.LineTo(
                    new Point(bounds.Right - cut, bounds.Bottom),
                    true,
                    false);

                context.LineTo(
                    new Point(bounds.X, bounds.Bottom),
                    true,
                    false);

                context.LineTo(
                    new Point(bounds.X, bounds.Y + cut),
                    true,
                    false);
            }

            geometry.Freeze();
            return geometry;
        }

        private void DrawTechButton(
            DrawingContext dc,
            Rect bounds,
            string text,
            bool hovered)
        {
            Geometry geometry = TechGeometry(bounds, 8);

            dc.DrawGeometry(
                BrushFor(hovered
                    ? Color.FromArgb(180, 35, 48, 85)
                    : Color.FromArgb(120, 15, 24, 48)),
                PenFor(
                    hovered
                        ? Gold
                        : Color.FromArgb(150, 0, 230, 255),
                    hovered ? 2 : 1.5),
                geometry);

            if (hovered)
            {
                dc.PushClip(geometry);

                dc.DrawLine(
                    PenFor(Gold, 3),
                    new Point(bounds.X + 3, bounds.Y + 6),
                    new Point(bounds.X + 3, bounds.Bottom - 6));

                double scanX = bounds.X +
                    (_stopwatch.Elapsed.TotalSeconds * 150 %
                        Math.Max(1, bounds.Width));

                dc.DrawRectangle(
                    BrushFor(Color.FromArgb(24, 255, 205, 80)),
                    null,
                    new Rect(
                        scanX - 8,
                        bounds.Y,
                        16,
                        bounds.Height));

                dc.Pop();
            }

            DrawTextFitted(
                dc,
                text,
                Math.Min(20, bounds.Height * 0.36),
                hovered ? Gold : White,
                new Rect(
                    bounds.X + 22,
                    bounds.Y + 3,
                    Math.Max(1, bounds.Width - 40),
                    Math.Max(1, bounds.Height - 6)),
                false);
        }

        private void DrawCornerMarkers(DrawingContext dc, double opacity)
        {
            dc.PushOpacity(opacity);

            double inset = 24;
            double length = 28;

            DrawCorner(dc, inset, inset, length, 1, 1);
            DrawCorner(dc, ActualWidth - inset, inset, length, -1, 1);
            DrawCorner(dc, inset, ActualHeight - inset, length, 1, -1);
            DrawCorner(
                dc,
                ActualWidth - inset,
                ActualHeight - inset,
                length,
                -1,
                -1);

            dc.Pop();
        }

        private void DrawCorner(
            DrawingContext dc,
            double x,
            double y,
            double size,
            double dx,
            double dy)
        {
            dc.DrawLine(
                PenFor(Cyan, 1.5),
                new Point(x, y),
                new Point(x + size * dx, y));

            dc.DrawLine(
                PenFor(Cyan, 1.5),
                new Point(x, y),
                new Point(x, y + size * dy));
        }

        private void DrawHangar(DrawingContext dc)
        {
            double width = Math.Min(850, ActualWidth * 0.84);
            double height = ActualHeight * 0.84;

            Rect panel = new Rect(
                (ActualWidth - width) / 2,
                (ActualHeight - height) / 2,
                width,
                height);

            DrawPanel(dc, panel);

            DrawCentered(
                dc,
                "HANGÁR",
                42,
                Gold,
                panel.X + panel.Width / 2,
                panel.Y + height * 0.045,
                width - 40);

            DrawCentered(
                dc,
                SkinData.Skins[_selectedSkin].Name,
                26,
                White,
                panel.X + panel.Width / 2,
                panel.Y + height * 0.15,
                width - 40);

            ImageSource image = _shipImages[_selectedSkin];

            Rect imageArea = new Rect(
                panel.X + width * 0.25,
                panel.Y + height * 0.23,
                width * 0.5,
                height * 0.38);

            if (image != null)
                dc.DrawImage(image, FitImage(image, imageArea));

            double buttonWidth = Math.Min(220, width * 0.35);
            double buttonHeight = Math.Min(56, height * 0.09);
            double gap = Math.Min(32, width * 0.06);
            double centerX = panel.X + width / 2;
            double buttonY = panel.Y + height * 0.68;

            AddMenuButton(
                MenuAction.PreviousSkin,
                new Rect(
                    centerX - gap / 2 - buttonWidth,
                    buttonY,
                    buttonWidth,
                    buttonHeight),
                "< ELŐZŐ");

            AddMenuButton(
                MenuAction.NextSkin,
                new Rect(
                    centerX + gap / 2,
                    buttonY,
                    buttonWidth,
                    buttonHeight),
                "KÖVETKEZŐ >");

            AddMenuButton(
                MenuAction.Back,
                new Rect(
                    centerX - buttonWidth / 2,
                    panel.Y + height * 0.83,
                    buttonWidth,
                    buttonHeight),
                "VISSZA");

            DrawMenuButtons(dc);
        }

        private void DrawHighScores(DrawingContext dc)
        {
            DrawCentered(
                dc,
                "RANGLISTA",
                48,
                Gold,
                ActualWidth / 2,
                ActualHeight * 0.08,
                ActualWidth - 40);

            for (int i = 0; i < _highScores.Count; i++)
            {
                HighScoreEntry entry = _highScores[i];

                DrawCentered(
                    dc,
                    (i + 1) + ". " + entry.Score +
                    " PONT | SZINT " + entry.Level +
                    " | BOSS " + entry.Bosses,
                    20,
                    White,
                    ActualWidth / 2,
                    ActualHeight * 0.2 + i * ActualHeight * 0.045,
                    ActualWidth * 0.9);
            }

            AddCenteredButton(
                MenuAction.ClearScores,
                "RANGLISTA TÖRLÉSE",
                ActualHeight * 0.79);

            AddCenteredButton(
                MenuAction.Back,
                "VISSZA",
                ActualHeight * 0.88);

            DrawMenuButtons(dc);
        }

        private void DrawBestiary(DrawingContext dc)
        {
            DrawCentered(
                dc,
                "ÖSSZESÍTÉS",
                48,
                Gold,
                ActualWidth / 2,
                ActualHeight * 0.1,
                ActualWidth - 40);

            for (int i = 0; i < BossData.Names.Length; i++)
            {
                DrawCentered(
                    dc,
                    BossData.Names[i],
                    25,
                    BossData.Colors[i],
                    ActualWidth / 2,
                    ActualHeight * 0.25 + i * ActualHeight * 0.085,
                    ActualWidth * 0.85);
            }

            AddCenteredButton(
                MenuAction.Back,
                "VISSZA",
                ActualHeight * 0.85);

            DrawMenuButtons(dc);
        }

        private void DrawGraphicsMenu(DrawingContext dc)
        {
            DrawCentered(
                dc,
                "GRAFIKA",
                48,
                Gold,
                ActualWidth / 2,
                ActualHeight * 0.1,
                ActualWidth - 40);

            MenuAction[] actions =
            {
                MenuAction.QualityLow,
                MenuAction.QualityMedium,
                MenuAction.QualityHigh,
                MenuAction.QualityUltra
            };

            string[] labels =
            {
                "ALACSONY",
                "KÖZEPES",
                "MAGAS",
                "ULTRA"
            };

            for (int i = 0; i < actions.Length; i++)
            {
                string label = labels[i];

                if ((int)_settings.Quality == i)
                    label += "  [AKTÍV]";

                AddCenteredButton(
                    actions[i],
                    label,
                    ActualHeight * (0.28 + i * 0.105));
            }

            AddCenteredButton(
                MenuAction.Back,
                "VISSZA",
                ActualHeight * 0.8);

            DrawMenuButtons(dc);
        }

        private void DrawGame(DrawingContext dc)
        {
            double shakeX = 0;
            double shakeY = 0;

            if (_settings.ScreenShake && _screenShake > 0)
            {
                shakeX = (_random.NextDouble() - 0.5) * _screenShake;
                shakeY = (_random.NextDouble() - 0.5) * _screenShake;
            }

            dc.PushTransform(new TranslateTransform(shakeX, shakeY));

            DrawAsteroids(dc);
            DrawBosses(dc);

            foreach (PowerUp powerUp in _powerUps)
            {
                dc.DrawEllipse(
                    BrushFor(Gold),
                    PenFor(Cyan, 1.5),
                    new Point(powerUp.X, powerUp.Y),
                    10,
                    10);
            }

            foreach (Bullet bullet in _bulletPool.Active)
            {
                dc.DrawRectangle(
                    BrushFor(WeaponSystem.GetBulletColor(bullet)),
                    null,
                    new Rect(
                        bullet.X,
                        bullet.Y,
                        bullet.Width,
                        bullet.Height));
            }

            foreach (EnemyBullet bullet in _enemyBulletPool.Active)
            {
                dc.DrawEllipse(
                    BrushFor(Red),
                    null,
                    new Point(bullet.X, bullet.Y),
                    bullet.Radius,
                    bullet.Radius);
            }

            foreach (Particle particle in _particlePool.Active)
            {
                dc.PushOpacity(Clamp(
                    particle.Life / Math.Max(0.001, particle.MaxLife),
                    0,
                    1));

                dc.DrawEllipse(
                    BrushFor(particle.Color),
                    null,
                    new Point(particle.X, particle.Y),
                    particle.Size,
                    particle.Size);

                dc.Pop();
            }

            DrawPlayer(dc);

            foreach (FloatingText text in _floatingTexts)
            {
                dc.PushOpacity(Clamp(
                    text.Life / Math.Max(0.001, text.MaxLife),
                    0,
                    1));

                DrawCentered(
                    dc,
                    text.Text,
                    18,
                    text.Color,
                    text.X,
                    text.Y,
                    300);

                dc.Pop();
            }

            dc.Pop();

            DrawHud(dc);

            if (_settings.ScreenFlash && _flashTime > 0)
            {
                dc.PushOpacity(Clamp(_flashTime * 2, 0, 1));

                dc.DrawRectangle(
                    BrushFor(Color.FromArgb(90, 255, 70, 120)),
                    null,
                    new Rect(0, 0, ActualWidth, ActualHeight));

                dc.Pop();
            }
        }

        private void DrawPlayer(DrawingContext dc)
        {
            double centerX = _playerX + PlayerWidth / 2;
            double centerY = _playerY + PlayerHeight / 2;
            ImageSource image = _shipImages[_selectedSkin];

            Rect imageBounds = new Rect(
                centerX - ShipDrawWidth / 2,
                centerY - ShipDrawHeight / 2,
                ShipDrawWidth,
                ShipDrawHeight);

            if (image != null)
                dc.DrawImage(image, FitImage(image, imageBounds));

            double heatY = imageBounds.Bottom + 8;

            DrawBar(
                dc,
                new Rect(centerX - 32, heatY, 64, 7),
                _weapons.Heat / Math.Max(1, _weapons.HeatMax),
                _weapons.IsOverheated ? Red : Gold);

            if (_weapons.IsOverheated)
            {
                DrawCentered(
                    dc,
                    "TÚLMELEGEDETT",
                    12,
                    Red,
                    centerX,
                    heatY + 12,
                    145);
            }
        }

        private void DrawAsteroids(DrawingContext dc)
        {
            foreach (Asteroid asteroid in _asteroids)
            {
                Color color = asteroid.Type == 0
                    ? Color.FromRgb(27, 23, 42)
                    : asteroid.Type == 1
                        ? Color.FromRgb(48, 38, 70)
                        : Color.FromRgb(70, 25, 55);

                StreamGeometry geometry = new StreamGeometry();

                using (StreamGeometryContext context = geometry.Open())
                {
                    for (int i = 0; i < asteroid.RadiusOffsets.Length; i++)
                    {
                        double angle = asteroid.Rotation +
                            i * Math.PI * 2 /
                            asteroid.RadiusOffsets.Length;

                        double radius = asteroid.Radius *
                            asteroid.RadiusOffsets[i];

                        Point point = new Point(
                            asteroid.X + Math.Cos(angle) * radius,
                            asteroid.Y + Math.Sin(angle) * radius);

                        if (i == 0)
                            context.BeginFigure(point, true, true);
                        else
                            context.LineTo(point, true, false);
                    }
                }

                dc.DrawGeometry(
                    BrushFor(color),
                    PenFor(Purple, 2),
                    geometry);

                if (asteroid.Health < asteroid.MaxHealth)
                {
                    DrawBar(
                        dc,
                        new Rect(
                            asteroid.X - asteroid.Radius,
                            asteroid.Y - asteroid.Radius - 12,
                            asteroid.Radius * 2,
                            5),
                        asteroid.Health / asteroid.MaxHealth,
                        Green);
                }
            }
        }

        private void DrawBosses(DrawingContext dc)
        {
            foreach (Boss boss in _bosses)
            {
                Color color = BossData.Colors[(int)boss.Kind];

                dc.DrawRectangle(
                    BrushFor(Color.FromArgb(
                        100,
                        color.R,
                        color.G,
                        color.B)),
                    PenFor(color, 3),
                    new Rect(
                        boss.X,
                        boss.Y,
                        boss.Width,
                        boss.Height));

                DrawBar(
                    dc,
                    new Rect(
                        boss.X,
                        boss.Y - 16,
                        boss.Width,
                        8),
                    boss.Health / boss.MaxHealth,
                    color);
            }
        }

        private void DrawHud(DrawingContext dc)
        {
            DrawBar(
                dc,
                new Rect(20, 20, 260, 20),
                _playerHealth / _playerMaxHealth,
                Green);

            DrawBar(
                dc,
                new Rect(20, 48, 200, 10),
                _shield / _shieldMax,
                Cyan);

            DrawBar(
                dc,
                new Rect(20, 66, 200, 7),
                _experience / (double)_experienceNeeded,
                Gold);

            DrawText(dc, "PONT: " + _score, 18, White, 20, 90);
            DrawText(dc, "SZINT: " + _level, 18, White, 20, 116);
            DrawText(dc, "HULLÁM: " + _wave, 18, White, 20, 142);

            DrawText(
                dc,
                "COMBO: " + _combat.Combo +
                "  x" + _combat.Multiplier.ToString(
                    "0.00",
                    CultureInfo.InvariantCulture),
                18,
                Gold,
                20,
                168);

            DrawText(
                dc,
                "DASH: " +
                (_dashCooldown <= 0
                    ? "KÉSZ"
                    : _dashCooldown.ToString("0.0")) +
                "  |  NOVA: " +
                (_novaCooldown <= 0
                    ? "KÉSZ"
                    : _novaCooldown.ToString("0.0")),
                16,
                Blue,
                20,
                ActualHeight - 35);
        }

        private void DrawBar(
            DrawingContext dc,
            Rect bounds,
            double ratio,
            Color color)
        {
            dc.DrawRectangle(
                BrushFor(Color.FromArgb(185, 0, 0, 0)),
                null,
                bounds);

            dc.DrawRectangle(
                BrushFor(color),
                null,
                new Rect(
                    bounds.X,
                    bounds.Y,
                    bounds.Width * Clamp(ratio, 0, 1),
                    bounds.Height));
        }

        private void DrawPauseMenu(DrawingContext dc)
        {
            DrawOverlay(dc);

            DrawCentered(
                dc,
                "SZÜNET",
                54,
                Gold,
                ActualWidth / 2,
                ActualHeight * 0.27,
                ActualWidth - 40);

            AddCenteredButton(
                MenuAction.Back,
                "FOLYTATÁS",
                ActualHeight * 0.43);

            AddCenteredButton(
                MenuAction.Exit,
                "KILÉPÉS",
                ActualHeight * 0.55);

            DrawMenuButtons(dc);
        }

        private void DrawUpgradeMenu(DrawingContext dc)
        {
            DrawOverlay(dc);

            DrawCentered(
                dc,
                "SZINTLÉPÉS",
                46,
                Gold,
                ActualWidth / 2,
                ActualHeight * 0.18,
                ActualWidth - 40);

            double width = Math.Min(620, ActualWidth * 0.85);

            for (int i = 0; i < _upgradeOptions.Count; i++)
            {
                UpgradeOption option = _upgradeOptions[i];

                AddMenuButton(
                    (MenuAction)((int)MenuAction.Upgrade0 + i),
                    new Rect(
                        (ActualWidth - width) / 2,
                        ActualHeight * (0.35 + i * 0.13),
                        width,
                        Math.Min(70, ActualHeight * 0.1)),
                    option.Title + " - " + option.Description);
            }

            DrawMenuButtons(dc);
        }

        private void DrawOverlay(DrawingContext dc)
        {
            dc.DrawRectangle(
                BrushFor(Color.FromArgb(215, 3, 5, 16)),
                null,
                new Rect(0, 0, ActualWidth, ActualHeight));
        }

        private void AddCenteredButton(
            MenuAction action,
            string text,
            double y)
        {
            double width = Math.Min(360, ActualWidth * 0.8);

            AddMenuButton(
                action,
                new Rect(
                    (ActualWidth - width) / 2,
                    y,
                    width,
                    Math.Min(58, ActualHeight * 0.085)),
                text);
        }

        private void AddMenuButton(
            MenuAction action,
            Rect bounds,
            string text)
        {
            _menuButtons.Add(new MenuButton
            {
                Action = action,
                Bounds = bounds,
                Text = text
            });
        }

        private void DrawMenuButtons(DrawingContext dc)
        {
            foreach (MenuButton button in _menuButtons)
            {
                DrawTechButton(
                    dc,
                    button.Bounds,
                    button.Text,
                    button.Bounds.Contains(_mousePosition));
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            _mousePosition = e.GetPosition(this);
        }

        private void OnMouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            Focus();

            if (_state == GameState.Boot)
            {
                BeginBootFadeOut();
                e.Handled = true;
                return;
            }

            if (_state == GameState.BootFadeOut)
                return;

            if (_state == GameState.MenuAwakening)
            {
                FinishMenuAnimation();
                e.Handled = true;
                return;
            }

            Point position = e.GetPosition(this);

            for (int i = 0; i < _menuButtons.Count; i++)
            {
                MenuButton button = _menuButtons[i];

                if (!button.Bounds.Contains(position))
                    continue;

                HandleMenuAction(button.Action);
                _menuButtons.Clear();
                e.Handled = true;
                return;
            }
        }

        private void HandleMenuAction(MenuAction action)
        {
            switch (action)
            {
                case MenuAction.Start:
                    StartGame();
                    break;

                case MenuAction.Hangar:
                    _state = GameState.Hangar;
                    break;

                case MenuAction.HighScores:
                    _state = GameState.HighScores;
                    break;

                case MenuAction.Bestiary:
                    _state = GameState.Bestiary;
                    break;

                case MenuAction.Graphics:
                    _state = GameState.Graphics;
                    break;

                case MenuAction.PreviousSkin:
                    ChangeSkin(-1);
                    break;

                case MenuAction.NextSkin:
                    ChangeSkin(1);
                    break;

                case MenuAction.ClearScores:
                    if (MessageBox.Show(
                        "Biztosan törlöd a ranglistát?",
                        "Ranglista törlése",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question) ==
                        MessageBoxResult.Yes)
                    {
                        _highScores.Clear();
                        SaveData(false);
                    }
                    break;

                case MenuAction.QualityLow:
                    SetGraphicsQuality(GraphicsQuality.Low);
                    break;

                case MenuAction.QualityMedium:
                    SetGraphicsQuality(GraphicsQuality.Medium);
                    break;

                case MenuAction.QualityHigh:
                    SetGraphicsQuality(GraphicsQuality.High);
                    break;

                case MenuAction.QualityUltra:
                    SetGraphicsQuality(GraphicsQuality.Ultra);
                    break;

                case MenuAction.Back:
                    _keys.Clear();

                    _state = _state == GameState.Paused
                        ? GameState.Playing
                        : GameState.Menu;
                    break;

                case MenuAction.Exit:
                    Application.Current.Shutdown();
                    break;

                case MenuAction.Upgrade0:
                    SelectUpgradeByIndex(0);
                    break;

                case MenuAction.Upgrade1:
                    SelectUpgradeByIndex(1);
                    break;

                case MenuAction.Upgrade2:
                    SelectUpgradeByIndex(2);
                    break;
            }
        }

        private void SetGraphicsQuality(GraphicsQuality quality)
        {
            _settings.Quality = quality;
            _settings.ApplyLimits();
            _settings.Save(SettingsPath);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_state == GameState.Boot)
            {
                if (!e.IsRepeat)
                    BeginBootFadeOut();

                e.Handled = true;
                return;
            }

            if (_state == GameState.BootFadeOut)
            {
                e.Handled = true;
                return;
            }

            if (_state == GameState.MenuAwakening)
            {
                if (e.Key == Key.Enter && !e.IsRepeat)
                    FinishMenuAnimation();

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape && !e.IsRepeat)
            {
                _keys.Clear();
                _menuButtons.Clear();

                if (_state == GameState.Playing)
                    _state = GameState.Paused;
                else if (_state == GameState.Paused)
                    _state = GameState.Playing;
                else if (_state != GameState.Upgrade)
                    _state = GameState.Menu;

                e.Handled = true;
                return;
            }

            if (_state == GameState.Hangar && !e.IsRepeat)
            {
                if (e.Key == Key.Left || e.Key == Key.A)
                    ChangeSkin(-1);
                else if (e.Key == Key.Right || e.Key == Key.D)
                    ChangeSkin(1);

                e.Handled = true;
                return;
            }

            if (_state == GameState.Upgrade && !e.IsRepeat)
            {
                if (e.Key == Key.D1 || e.Key == Key.NumPad1)
                    SelectUpgradeByIndex(0);
                else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
                    SelectUpgradeByIndex(1);
                else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
                    SelectUpgradeByIndex(2);

                e.Handled = true;
                return;
            }

            if (_state != GameState.Playing)
                return;

            _keys.Add(e.Key);

            if (!e.IsRepeat)
            {
                if ((e.Key == Key.LeftShift ||
                     e.Key == Key.RightShift) &&
                    _dashCooldown <= 0)
                {
                    _dashTime = 0.16;
                    _dashCooldown = 1.6;
                    _invulnerabilityTime = Math.Max(
                        _invulnerabilityTime,
                        0.16);
                }
                else if (e.Key == Key.Q)
                {
                    ActivateNova();
                }
                else if (e.Key == Key.E)
                {
                    _weapons.CycleWeapon();
                }
            }

            e.Handled = true;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            _keys.Remove(e.Key);
            e.Handled = true;
        }

        private SolidColorBrush BrushFor(Color color)
        {
            SolidColorBrush brush;

            if (_brushes.TryGetValue(color, out brush))
                return brush;

            brush = new SolidColorBrush(color);
            brush.Freeze();
            _brushes[color] = brush;

            return brush;
        }

        private Pen PenFor(Color color, double thickness)
        {
            string key = color.ToString() + "|" +
                thickness.ToString(CultureInfo.InvariantCulture);

            Pen pen;

            if (_pens.TryGetValue(key, out pen))
                return pen;

            pen = new Pen(BrushFor(color), thickness);
            pen.Freeze();
            _pens[key] = pen;

            return pen;
        }

        private FormattedText CreateText(
            string text,
            double size,
            Color color)
        {
            return new FormattedText(
                text ?? string.Empty,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                _typeface,
                Math.Max(1, size),
                BrushFor(color),
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
        }

        private void DrawText(
            DrawingContext dc,
            string text,
            double size,
            Color color,
            double x,
            double y)
        {
            if (string.IsNullOrEmpty(text))
                return;

            dc.DrawText(
                CreateText(text, size, color),
                new Point(x, y));
        }

        private void DrawCentered(
            DrawingContext dc,
            string text,
            double size,
            Color color,
            double centerX,
            double y,
            double maxWidth)
        {
            if (string.IsNullOrEmpty(text))
                return;

            FormattedText formatted = CreateText(text, size, color);

            if (formatted.Width > maxWidth && formatted.Width > 0)
            {
                formatted = CreateText(
                    text,
                    size * Math.Max(1, maxWidth) / formatted.Width,
                    color);
            }

            dc.DrawText(
                formatted,
                new Point(
                    centerX - formatted.Width / 2,
                    y));
        }

        private void DrawTextFitted(
            DrawingContext dc,
            string text,
            double size,
            Color color,
            Rect bounds,
            bool centered)
        {
            if (string.IsNullOrEmpty(text))
                return;

            FormattedText formatted = CreateText(text, size, color);

            double factor = Math.Min(
                1,
                Math.Min(
                    bounds.Width / Math.Max(1, formatted.Width),
                    bounds.Height / Math.Max(1, formatted.Height)));

            if (factor < 1)
                formatted = CreateText(text, size * factor, color);

            dc.DrawText(
                formatted,
                new Point(
                    centered
                        ? bounds.X + (bounds.Width - formatted.Width) / 2
                        : bounds.X,
                    bounds.Y + (bounds.Height - formatted.Height) / 2));
        }

        private static Rect FitImage(
            ImageSource image,
            Rect bounds)
        {
            if (image.Width <= 0 || image.Height <= 0)
                return bounds;

            double scale = Math.Min(
                bounds.Width / image.Width,
                bounds.Height / image.Height);

            double width = image.Width * scale;
            double height = image.Height * scale;

            return new Rect(
                bounds.X + (bounds.Width - width) / 2,
                bounds.Y + (bounds.Height - height) / 2,
                width,
                height);
        }

        private static string RevealText(
            string text,
            double progress)
        {
            int count = (int)(
                text.Length * Clamp(progress, 0, 1));

            return text.Substring(0, Math.Min(text.Length, count));
        }

        private static double EaseOut(double value)
        {
            value = Clamp(value, 0, 1);
            return 1 - Math.Pow(1 - value, 3);
        }

        private static double Clamp(
            double value,
            double minimum,
            double maximum)
        {
            return value < minimum
                ? minimum
                : value > maximum ? maximum : value;
        }
    }
}