﻿using Autofac;
using CQELight.Implementations.IoC;
using CQELight.Tools.Extensions;

namespace CQELight.IoC.Autofac
{
    internal static class AutofacTools
    {
        #region static methods

        public static void RegisterContextTypes(ContainerBuilder b, TypeRegister typeRegister)
        {
            typeRegister.Objects.DoForEach(o =>
            {
                if (o != null)
                {
                    b.RegisterInstance(o)
                       .AsImplementedInterfaces()
                       .AsSelf();
                }
            });
            typeRegister.Types.DoForEach(t =>
            {
                if (t != null)
                {
                    b.RegisterType(t)
                       .AsImplementedInterfaces()
                       .AsSelf()
                       .FindConstructorsWith(new FullConstructorFinder());
                }
            });
            typeRegister.ObjAsTypes.DoForEach(kvp =>
            {
                if (kvp.Key != null)
                {
                    b.RegisterInstance(kvp.Key)
                       .As(kvp.Value)
                       .AsImplementedInterfaces()
                       .AsSelf();
                }
            });
            typeRegister.TypeAsTypes.DoForEach(kvp =>
            {
                if (kvp.Key != null)
                {
                    b.RegisterType(kvp.Key)
                       .As(kvp.Value)
                       .AsImplementedInterfaces()
                       .AsSelf()
                       .FindConstructorsWith(new FullConstructorFinder());
                }
            });
        }

        #endregion
    }
}
