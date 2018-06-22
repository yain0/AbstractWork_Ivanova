﻿namespace AbstractWorkModel
{
    /// <summary>
    /// Сколько компонентов хранится на складе
    /// </summary>
    public class SkladMaterial
    {
        public int Id { get; set; }

        public int SkladId { get; set; }

        public int MaterialId { get; set; }

        public int Koll { get; set; }
        public virtual Sklad Sklad { get; set; }
        public virtual Material Material { get; set; }
    }
}
