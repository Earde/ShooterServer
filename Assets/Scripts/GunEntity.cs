using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    /// <summary>
    /// Stats of gun
    /// </summary>
    class GunEntity
    {
        public float headDamage { get; set; }
        public float torsoDamage { get; set; }
        public float hipsDamage { get; set; }
        public float armsDamage { get; set; }
        public float legsDamage { get; set; }
        public float equipCooldown { get; set; }
        public float shotCooldown { get; set; }
    }
}
