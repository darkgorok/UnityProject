using System;
using UnityEngine;
using Zenject;

public sealed class LevelInstaller : MonoInstaller
{
    [SerializeField] private LevelFacade facade;

    public override void InstallBindings()
    {
        if (facade == null)
            throw new InvalidOperationException("LevelInstaller: facade is not assigned.");
        if (facade.Door == null)
            throw new InvalidOperationException("LevelInstaller: Door is not assigned on LevelFacade.");
        if (facade.AimProvider == null)
            throw new InvalidOperationException("LevelInstaller: AimProvider is not assigned on LevelFacade.");

        Container.Bind<LevelFacade>().FromInstance(facade).AsSingle();
        Container.Bind<Transform>().WithId("Goal").FromInstance(facade.Door.transform).AsCached();
        Container.Bind<IAimDirectionProvider>().FromInstance(facade.AimProvider).AsSingle();
    }
}
