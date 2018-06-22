﻿using AbstractWorkModel;
using AbstractWorkService.BindingModels;
using AbstractWorkService.Interfaces;
using AbstractWorkService.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Data.Entity;

namespace AbstractWorkService.WorkationsBD
{
    public class MyServiceBD : IMyService
    {
        private AbstractDbContext context;

        public MyServiceBD(AbstractDbContext context)
        {
            this.context = context;
        }

        public List<ActivityViewModel> GetList()
        {
            List<ActivityViewModel> result = context.Activitys
                .Select(rec => new ActivityViewModel
                {
                    Id = rec.Id,
                    CustomerId = rec.CustomerId,
                    RemontId = rec.RemontId,
                    WorkerId = rec.WorkerId,
                    DateCreate = SqlFunctions.DateName("dd", rec.DateCreate) + " " +
                                SqlFunctions.DateName("mm", rec.DateCreate) + " " +
                                SqlFunctions.DateName("yyyy", rec.DateCreate),
                    DateWork = rec.DateWork == null ? "" :
                                        SqlFunctions.DateName("dd", rec.DateWork.Value) + " " +
                                        SqlFunctions.DateName("mm", rec.DateWork.Value) + " " +
                                        SqlFunctions.DateName("yyyy", rec.DateWork.Value),
                    Status = rec.Status.ToString(),
                    Koll = rec.Koll,
                    Summa = rec.Summa,
                    CustomerFIO = rec.Customer.CustomerFIO,
                    RemontName = rec.Remont.RemontName,
                    WorkerName = rec.Worker.WorkerFIO
                })
                .ToList();
            return result;
        }

        public void CreateActivity(ActivityBindingModel model)
        {
            context.Activitys.Add(new Activity
            {
                CustomerId = model.CustomerId,
                RemontId = model.RemontId,
                DateCreate = DateTime.Now,
                Koll = model.Koll,
                Summa = model.Summa,
                Status = ActivityStatus.Принят
            });
            context.SaveChanges();
        }

        public void TakeActivityInWork(ActivityBindingModel model)
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                try
                {

                    Activity element = context.Activitys.FirstOrDefault(rec => rec.Id == model.Id);
                    if (element == null)
                    {
                        throw new Exception("Элемент не найден");
                    }
                    var RemontMaterials = context.RemontMaterials
                                                .Include(rec => rec.Material)
                                                .Where(rec => rec.RemontId == element.RemontId);
                    // списываем
                    foreach (var RemontMaterial in RemontMaterials)
                    {
                        int countOnSklads = RemontMaterial.Koll * element.Koll;
                        var SkladMaterials = context.SkladMaterials
                                                    .Where(rec => rec.MaterialId == RemontMaterial.MaterialId);
                        foreach (var SkladMaterial in SkladMaterials)
                        {
                            // компонентов на одном слкаде может не хватать
                            if (SkladMaterial.Koll >= countOnSklads)
                            {
                                SkladMaterial.Koll -= countOnSklads;
                                countOnSklads = 0;
                                context.SaveChanges();
                                break;
                            }
                            else
                            {
                                countOnSklads -= SkladMaterial.Koll;
                                SkladMaterial.Koll = 0;
                                context.SaveChanges();
                            }
                        }
                        if (countOnSklads > 0)
                        {
                            throw new Exception("Не достаточно компонента " +
                                RemontMaterial.Material.MaterialName + " требуется " +
                                RemontMaterial.Koll + ", не хватает " + countOnSklads);
                        }
                    }
                    element.WorkerId = model.WorkerId;
                    element.DateWork = DateTime.Now;
                    element.Status = ActivityStatus.Выполняется;
                    context.SaveChanges();
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void FinishActivity(int id)
        {
            Activity element = context.Activitys.FirstOrDefault(rec => rec.Id == id);
            if (element == null)
            {
                throw new Exception("Элемент не найден");
            }
            element.Status = ActivityStatus.Готов;
            context.SaveChanges();
        }

        public void PayActivity(int id)
        {
            Activity element = context.Activitys.FirstOrDefault(rec => rec.Id == id);
            if (element == null)
            {
                throw new Exception("Элемент не найден");
            }
            element.Status = ActivityStatus.Оплачен;
            context.SaveChanges();
        }

        public void PutMaterialOnSklad(SkladMaterialBindingModel model)
        {
            SkladMaterial element = context.SkladMaterials
                                                .FirstOrDefault(rec => rec.SkladId == model.SkladId &&
                                                                    rec.MaterialId == model.MaterialId);
            if (element != null)
            {
                element.Koll += model.Koll;
            }
            else
            {
                context.SkladMaterials.Add(new SkladMaterial
                {
                    SkladId = model.SkladId,
                    MaterialId = model.MaterialId,
                    Koll = model.Koll
                });
            }
            context.SaveChanges();
        }
    }
}
