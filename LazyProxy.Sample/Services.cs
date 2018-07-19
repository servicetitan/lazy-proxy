using System;
using System.Linq;

namespace LazyProxy.Sample
{
    public interface IMyService
    {
        void Foo();
    }

    public class MyService : IMyService
    {
        public MyService() => Console.WriteLine("Hello from ctor");
        public void Foo() => Console.WriteLine("Hello from Foo");
    }

    public abstract class Warrior
    {
        public Weapon[] Weapons { get; set; }
    }

    public class Ninja : Warrior
    {
        public int Health { get; set; }

        public Ninja(int health, params Weapon[] weapons)
        {
            Health = health;
            Weapons = weapons;
        }
    }

    public class Weapon
    {
        public int Damage { get; set; }

        public Weapon(int damage)
        {
            Damage = damage;
        }
    }

    public interface IWarriorService
    {
        int GetDamage(Warrior warrior);
    }

    public interface INinjaService : IWarriorService
    {
        int MinNinjaHealth { get; set; }
        Ninja CreateNinja();
    }

    public interface IWeaponService
    {
        int MinWeaponDamage { get; set; }
        Weapon CreateSword();
        Weapon CreateShuriken();
    }

    public class NinjaService : INinjaService
    {
        private readonly IWeaponService _weaponService;

        public int MinNinjaHealth { get; set; } = 50;

        public NinjaService(IWeaponService weaponService)
        {
            Console.WriteLine("ctor: NinjaService");
            _weaponService = weaponService;
        }

        public int GetDamage(Warrior warrior)
        {
            if (warrior == null)
            {
                throw new ArgumentNullException();
            }

            return warrior.Weapons.Sum(w => w.Damage);
        }

        public Ninja CreateNinja()
        {
            var sword = _weaponService.CreateSword();
            var shuriken = _weaponService.CreateShuriken();
            var health = MinNinjaHealth + 10;

            return new Ninja(health, sword, shuriken);
        }
    }

    public class WeaponService : IWeaponService
    {
        public int MinWeaponDamage { get; set; } = 10;

        public WeaponService()
        {
            Console.WriteLine("ctor: WeaponService");
        }

        public Weapon CreateSword()
        {
            var damage = MinWeaponDamage + 10;
            return new Weapon(damage);
        }

        public Weapon CreateShuriken()
        {
            var damage = MinWeaponDamage + 5;
            return new Weapon(damage);
        }
    }
}