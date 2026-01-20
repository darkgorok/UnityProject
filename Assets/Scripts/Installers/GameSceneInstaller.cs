using UnityEngine;
using Zenject;

public class GameSceneInstaller : MonoInstaller
{
    [Header("Level Prefab")]
    [SerializeField] private GameObject levelPrefab;
    [SerializeField] private LevelFlowConfig levelFlowConfig;
    [Header("Player Prefab")]
    [SerializeField] private GameObject playerPrefab;
    [Header("UI Prefabs")]
    [SerializeField] private GameObject winScreenPrefab;
    [SerializeField] private GameObject loseScreenPrefab;
    [SerializeField] private Transform uiRoot;
    [Header("Projectile Pool")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private int projectilePoolSize = 16;

    public override void InstallBindings()
    {
        // No global signals used.

        Container.Bind<GameFlowModel>().AsSingle().IfNotBound();
        Container.Bind<IFailController>().To<FailController>().AsSingle().IfNotBound();
        Container.BindInterfacesAndSelfTo<GameFlowController>().AsSingle().NonLazy().IfNotBound();
        Container.Bind<IDoor>().To<DoorModel>().AsSingle().IfNotBound();
        Container.Bind<ITimeProvider>().To<UnityTimeProvider>().AsSingle().IfNotBound();
        Container.Bind<ILevelReloader>().To<SceneLevelReloader>().AsSingle().IfNotBound();
#if UNITY_EDITOR
        Container.Bind<IInputService>().To<EditorInputService>().AsSingle().IfNotBound();
#elif UNITY_IOS || UNITY_ANDROID
        Container.Bind<IInputService>().To<MobileInputService>().AsSingle().IfNotBound();
#else
        Container.Bind<IInputService>().To<UnityInputService>().AsSingle().IfNotBound();
#endif
        Container.Bind<PathState>().AsSingle().IfNotBound();
        Container.BindInterfacesTo<ObstacleRegistryService>().AsSingle().IfNotBound();
        Container.BindInterfacesAndSelfTo<GoalService>().AsSingle().NonLazy().IfNotBound();
        if (levelFlowConfig != null)
            Container.Bind<LevelFlowConfig>().FromInstance(levelFlowConfig).AsSingle().IfNotBound();

        if (projectilePrefab != null)
        {
            Container.BindMemoryPool<Projectile, Projectile.Pool>()
                .WithInitialSize(projectilePoolSize)
                .FromComponentInNewPrefab(projectilePrefab)
                .UnderTransformGroup("Projectiles");
        }

        if (levelPrefab != null)
        {
            Container.Bind<LevelFacade>()
                .FromSubContainerResolve()
                .ByNewContextPrefab(levelPrefab)
                .AsSingle();

            Container.Bind<Transform>().WithId("Goal")
                .FromResolveGetter<LevelFacade>(facade => facade.Door.transform)
                .AsCached()
                .IfNotBound();

            Container.Bind<IAimDirectionProvider>()
                .FromResolveGetter<LevelFacade>(facade => facade.AimProvider)
                .AsSingle()
                .IfNotBound();
        }
        else
        {
            Debug.LogError("GameSceneInstaller: levelPrefab is not assigned.", this);
        }

        if (playerPrefab != null)
        {
            Container.BindInterfacesAndSelfTo<PlayerShooting>().FromComponentInNewPrefab(playerPrefab).AsSingle().NonLazy();
            Container.Bind<Transform>().WithId("Player")
                .FromResolveGetter<PlayerShooting>(shooting => shooting.transform)
                .AsCached()
                .IfNotBound();
            Container.BindInterfacesAndSelfTo<PlayerShootInput>()
                .FromResolveGetter<PlayerShooting, PlayerShootInput>(shooting => shooting.ShootInput)
                .AsSingle()
                .NonLazy();
            Container.Bind<PlayerShootGate>()
                .FromResolveGetter<PlayerShooting>(shooting => shooting.ShootGate)
                .AsSingle();
            Container.Bind<PlayerFailWatcher>()
                .FromResolveGetter<PlayerShooting>(shooting => shooting.FailWatcher)
                .AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerMovement>()
                .FromResolveGetter<PlayerShooting, PlayerMovement>(shooting => shooting.Movement)
                .AsSingle()
                .NonLazy();
        }
        else
        {
            Container.BindInterfacesAndSelfTo<PlayerShooting>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<Transform>().WithId("Player")
                .FromResolveGetter<PlayerShooting>(shooting => shooting.transform)
                .AsCached()
                .IfNotBound();
            Container.BindInterfacesAndSelfTo<PlayerShootInput>().FromComponentInHierarchy().AsSingle().NonLazy();
            Container.Bind<PlayerShootGate>().FromComponentInHierarchy().AsSingle();
            Container.Bind<PlayerFailWatcher>().FromComponentInHierarchy().AsSingle();
            Container.BindInterfacesAndSelfTo<PlayerMovement>().FromComponentInHierarchy().AsSingle().NonLazy();
        }

        Container.BindInterfacesAndSelfTo<GameUiPresenter>().AsSingle().NonLazy();
        if (uiRoot != null)
            Container.Bind<Transform>().WithId("UiRoot").FromInstance(uiRoot).AsSingle().IfNotBound();

        if (winScreenPrefab != null)
        {
            Container.Bind<IResultScreen>().WithId("Win")
                .To<WinScreenView>()
                .FromComponentInNewPrefab(winScreenPrefab)
                .AsSingle()
                .NonLazy();
        }
        else
        {
            Container.Bind<IResultScreen>().WithId("Win").To<WinScreenView>().FromComponentInHierarchy().AsSingle().IfNotBound();
        }

        if (loseScreenPrefab != null)
        {
            Container.Bind<IResultScreen>().WithId("Lose")
                .To<LoseScreenView>()
                .FromComponentInNewPrefab(loseScreenPrefab)
                .AsSingle()
                .NonLazy();
        }
        else
        {
            Container.Bind<IResultScreen>().WithId("Lose").To<LoseScreenView>().FromComponentInHierarchy().AsSingle().IfNotBound();
        }
    }
}
