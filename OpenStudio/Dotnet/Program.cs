
using EnergyPlus.HVACTemplates;
using EnergyPlus.InternalGains;
using EnergyPlus.LocationandClimate;
using EnergyPlus.OutputReporting;
using EnergyPlus.Schedules;
using EnergyPlus.SimulationParameters;
using EnergyPlus.SurfaceConstructionElements;
using EnergyPlus.ThermalZonesandSurfaces;
using EnergyPlus.ZoneHVACControlsandThermostats;
using System;
using System.Collections.Generic;
using BH.oM.Architecture;
using EnergyPlus;

namespace BuildingEnergySimulation
{
    class Program
    {
        static void Main(string[] args)
        {
            // EnergyPlus 모델 생성
            var eplusModel = new EnergyPlusModel();

            // 1. 기본 시뮬레이션 설정
            CreateSimulationControl(eplusModel);
            CreateBuilding(eplusModel);
            CreateGlobalGeometryRules(eplusModel);
            CreateTimestep(eplusModel);
            CreateRunPeriod(eplusModel);

            // 2. 재료 및 구조체 정의
            CreateMaterials(eplusModel);
            CreateConstructions(eplusModel);

            // 3. 스케줄 정의
            CreateSchedules(eplusModel);

            // 4. Zone 생성 (방1, 방2)
            CreateZones(eplusModel);

            // 5. 표면(벽, 바닥, 천장, 창문) 생성
            CreateSurfaces(eplusModel);

            // 6. 내부 부하 정의
            CreateInternalLoads(eplusModel);

            // 7. HVAC 시스템 (Ideal Loads Air System)
            CreateHVACSystem(eplusModel);

            // 8. 출력 설정
            CreateOutputs(eplusModel);

            // 9. IDF 파일로 저장
            SaveToFile(eplusModel, "TwoRoomBuilding.idf");

            Console.WriteLine("EnergyPlus 모델이 성공적으로 생성되었습니다: TwoRoomBuilding.idf");
        }

        static void CreateSimulationControl(EnergyPlusModel model)
        {
            model.SimulationControl = new SimulationControl
            {
                DoZoneSizingCalculation = "Yes",
                DoSystemSizingCalculation = "Yes",
                DoPlantSizingCalculation = "No",
                RunSimulationforSizingPeriods = "No",
                RunSimulationforWeatherFileRunPeriods = "Yes"
            };
        }

        static void CreateBuilding(EnergyPlusModel model)
        {
            model.Building = new Building
            {
                NodeName = "TwoRoomBuilding",
                NorthAxis = 0.0,
                Terrain = """Suburbs""",
                LoadsConvergenceToleranceValue = 0.04,
                TemperatureConvergenceToleranceValue = 0.4,
                SolarDistribution = "FullExterior",
                MaximumNumberOfWarmupDays = 25,
                MinimumNumberOfWarmupDays = 6
            };
        }

        static void CreateGlobalGeometryRules(EnergyPlusModel model)
        {
            model.GlobalGeometryRules = new GlobalGeometryRules
            {
                StartingVertexPosition = "UpperLeftCorner",
                VertexEntryDirection = "Counterclockwise",
                CoordinateSystem = "Relative",
                DaylightingReferencePointCoordinateSystem = "Relative",
                RectangularSurfaceCoordinateSystem = "Relative"
            };
        }

        static void CreateTimestep(EnergyPlusModel model)
        {
            model.Timestep = new Timestep
            {
                NumberOfTimestepsPerHour = 4
            };
        }

        static void CreateRunPeriod(EnergyPlusModel model)
        {
            model.RunPeriods.Add(new RunPeriod
            {
                NodeName = "AnnualRun",
                BeginMonth = 1,
                BeginDayofMonth = 1,
                EndMonth = 12,
                EndDayofMonth = 31,
                DayofWeekforStartDay = "Sunday",
                UseWeatherFileHolidays = "Yes",
                UseWeatherFileDaylightSavings = "Yes",
                ApplyWeekendHolidayRule = "No",
                UseWeatherFileRainIndicators = "Yes",
                UseWeatherFileSnowIndicators = "Yes"
            });
        }

        static void CreateMaterials(EnergyPlusModel model)
        {
            // 콘크리트
            model.Materials.Add(new Material
            {
                Name = "Concrete",
                Roughness = "MediumRough",
                Thickness = 0.2,
                Conductivity = 1.95,
                Density = 2400,
                SpecificHeat = 900
            });

            // 단열재
            model.Materials.Add(new Material
            {
                Name = "Insulation",
                Roughness = "MediumRough",
                Thickness = 0.08,
                Conductivity = 0.04,
                Density = 30,
                SpecificHeat = 1300
            });

            // 석고보드
            model.Materials.Add(new Material
            {
                Name = "Gypsum",
                Roughness = "MediumSmooth",
                Thickness = 0.013,
                Conductivity = 0.16,
                Density = 800,
                SpecificHeat = 1090
            });

            // 유리창
            model.WindowMaterials.Add(new WindowMaterialSimpleGlazingSystem
            {
                Name = "SimpleWindow",
                UFactor = 2.7,
                SolarHeatGainCoefficient = 0.763
            });
        }

        static void CreateConstructions(EnergyPlusModel model)
        {
            // 외벽 구조
            model.Constructions.Add(new Construction
            {
                Name = "ExteriorWall",
                LayerNames = new List<string>
                {
                    "Concrete",
                    "Insulation",
                    "Gypsum"
                }
            });

            // 내벽 구조
            model.Constructions.Add(new Construction
            {
                Name = "InteriorWall",
                LayerNames = new List<string>
                {
                    "Gypsum",
                    "Concrete",
                    "Gypsum"
                }
            });

            // 바닥/천장 구조
            model.Constructions.Add(new Construction
            {
                Name = "FloorCeiling",
                LayerNames = new List<string>
                {
                    "Concrete",
                    "Insulation",
                    "Gypsum"
                }
            });

            // 창문 구조
            model.Constructions.Add(new Construction
            {
                Name = "WindowConstruction",
                LayerNames = new List<string> { "SimpleWindow" }
            });
        }

        static void CreateSchedules(EnergyPlusModel model)
        {
            // 평일 8-18시 운영 스케줄
            model.ScheduleCompacts.Add(new ScheduleCompact
            {
                Name = "OccupancySchedule",
                ScheduleTypeLimitsName = "Fraction",
                FieldSet = new List<string>
                {
                    "Through: 12/31",
                    "For: Weekdays",
                    "Until: 08:00", "0.0",
                    "Until: 18:00", "1.0",
                    "Until: 24:00", "0.0",
                    "For: AllOtherDays",
                    "Until: 24:00", "0.0"
                }
            });

            // 조명 스케줄 (재실과 동일)
            model.ScheduleCompacts.Add(new ScheduleCompact
            {
                Name = "LightingSchedule",
                ScheduleTypeLimitsName = "Fraction",
                FieldSet = new List<string>
                {
                    "Through: 12/31",
                    "For: Weekdays",
                    "Until: 08:00", "0.1",
                    "Until: 18:00", "1.0",
                    "Until: 24:00", "0.1",
                    "For: AllOtherDays",
                    "Until: 24:00", "0.1"
                }
            });

            // 사무기기 스케줄
            model.ScheduleCompacts.Add(new ScheduleCompact
            {
                Name = "EquipmentSchedule",
                ScheduleTypeLimitsName = "Fraction",
                FieldSet = new List<string>
                {
                    "Through: 12/31",
                    "For: Weekdays",
                    "Until: 08:00", "0.3",
                    "Until: 18:00", "1.0",
                    "Until: 24:00", "0.3",
                    "For: AllOtherDays",
                    "Until: 24:00", "0.3"
                }
            });

            // 항상 켜진 스케줄
            model.ScheduleCompacts.Add(new ScheduleCompact
            {
                Name = "AlwaysOn",
                ScheduleTypeLimitsName = "Fraction",
                FieldSet = new List<string>
                {
                    "Through: 12/31",
                    "For: AllDays",
                    "Until: 24:00", "1.0"
                }
            });
        }

        static void CreateZones(EnergyPlusModel model)
        {
            // 방1 (3m x 4m x 3m)
            model.Zones.Add(new Zone
            {
                Name = "Room1",
                DirectionofRelativeNorth = 0.0,
                XOrigin = 0.0,
                YOrigin = 0.0,
                ZOrigin = 0.0,
                Type = 1,
                Multiplier = 1,
                CeilingHeight = 3.0,
                Volume = 36.0, // 3 * 4 * 3
                FloorArea = 12.0 // 3 * 4
            });

            // 방2 (3m x 5m x 3m)
            model.Zones.Add(new Zone
            {
                Name = "Room2",
                DirectionofRelativeNorth = 0.0,
                XOrigin = 4.0, // 방1 동쪽에 위치
                YOrigin = 0.0,
                ZOrigin = 0.0,
                Type = 1,
                Multiplier = 1,
                CeilingHeight = 3.0,
                Volume = 45.0, // 3 * 5 * 3
                FloorArea = 15.0 // 3 * 5
            });
        }

        static void CreateSurfaces(EnergyPlusModel model)
        {
            // 방1 표면
            // 방1 바닥
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room1_Floor",
                SurfaceType = "Floor",
                ConstructionName = "FloorCeiling",
                ZoneName = "Room1",
                OutsideBoundaryCondition = "Ground",
                SunExposure = "NoSun",
                WindExposure = "NoWind",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 0, Y = 0, Z = 0 },
                    new Vertex { X = 4, Y = 0, Z = 0 },
                    new Vertex { X = 4, Y = 3, Z = 0 },
                    new Vertex { X = 0, Y = 3, Z = 0 }
                }
            });

            // 방1 천장
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room1_Ceiling",
                SurfaceType = "Roof",
                ConstructionName = "FloorCeiling",
                ZoneName = "Room1",
                OutsideBoundaryCondition = "Outdoors",
                SunExposure = "SunExposed",
                WindExposure = "WindExposed",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 0, Y = 3, Z = 3 },
                    new Vertex { X = 4, Y = 3, Z = 3 },
                    new Vertex { X = 4, Y = 0, Z = 3 },
                    new Vertex { X = 0, Y = 0, Z = 3 }
                }
            });

            // 방1 서쪽 벽 (창문 포함)
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room1_WestWall",
                SurfaceType = "Wall",
                ConstructionName = "ExteriorWall",
                ZoneName = "Room1",
                OutsideBoundaryCondition = "Outdoors",
                SunExposure = "SunExposed",
                WindExposure = "WindExposed",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 0, Y = 3, Z = 3 },
                    new Vertex { X = 0, Y = 3, Z = 0 },
                    new Vertex { X = 0, Y = 0, Z = 0 },
                    new Vertex { X = 0, Y = 0, Z = 3 }
                }
            });

            // 서향 창문 (2m x 1.5m)
            model.FenestrationSurfaceDetaileds.Add(new FenestrationSurfaceDetailed
            {
                Name = "Room1_WestWindow",
                SurfaceType = "Window",
                ConstructionName = "WindowConstruction",
                BuildingSurfaceName = "Room1_WestWall",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 0, Y = 2.5, Z = 2.5 },
                    new Vertex { X = 0, Y = 2.5, Z = 1.0 },
                    new Vertex { X = 0, Y = 0.5, Z = 1.0 },
                    new Vertex { X = 0, Y = 0.5, Z = 2.5 }
                }
            });

            // 방1 남쪽 벽
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room1_SouthWall",
                SurfaceType = "Wall",
                ConstructionName = "ExteriorWall",
                ZoneName = "Room1",
                OutsideBoundaryCondition = "Outdoors",
                SunExposure = "SunExposed",
                WindExposure = "WindExposed",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 0, Y = 0, Z = 3 },
                    new Vertex { X = 0, Y = 0, Z = 0 },
                    new Vertex { X = 4, Y = 0, Z = 0 },
                    new Vertex { X = 4, Y = 0, Z = 3 }
                }
            });

            // 방1 북쪽 벽
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room1_NorthWall",
                SurfaceType = "Wall",
                ConstructionName = "ExteriorWall",
                ZoneName = "Room1",
                OutsideBoundaryCondition = "Outdoors",
                SunExposure = "SunExposed",
                WindExposure = "WindExposed",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 4, Y = 3, Z = 3 },
                    new Vertex { X = 4, Y = 3, Z = 0 },
                    new Vertex { X = 0, Y = 3, Z = 0 },
                    new Vertex { X = 0, Y = 3, Z = 3 }
                }
            });

            // 방1과 방2 사이 내벽 (방1 쪽)
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room1_EastWall",
                SurfaceType = "Wall",
                ConstructionName = "InteriorWall",
                ZoneName = "Room1",
                OutsideBoundaryCondition = "Surface",
                OutsideBoundaryConditionObject = "Room2_WestWall",
                SunExposure = "NoSun",
                WindExposure = "NoWind",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 4, Y = 0, Z = 3 },
                    new Vertex { X = 4, Y = 0, Z = 0 },
                    new Vertex { X = 4, Y = 3, Z = 0 },
                    new Vertex { X = 4, Y = 3, Z = 3 }
                }
            });

            // 방2 표면
            // 방2 바닥
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room2_Floor",
                SurfaceType = "Floor",
                ConstructionName = "FloorCeiling",
                ZoneName = "Room2",
                OutsideBoundaryCondition = "Ground",
                SunExposure = "NoSun",
                WindExposure = "NoWind",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 4, Y = 0, Z = 0 },
                    new Vertex { X = 9, Y = 0, Z = 0 },
                    new Vertex { X = 9, Y = 3, Z = 0 },
                    new Vertex { X = 4, Y = 3, Z = 0 }
                }
            });

            // 방2 천장
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room2_Ceiling",
                SurfaceType = "Roof",
                ConstructionName = "FloorCeiling",
                ZoneName = "Room2",
                OutsideBoundaryCondition = "Outdoors",
                SunExposure = "SunExposed",
                WindExposure = "WindExposed",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 4, Y = 3, Z = 3 },
                    new Vertex { X = 9, Y = 3, Z = 3 },
                    new Vertex { X = 9, Y = 0, Z = 3 },
                    new Vertex { X = 4, Y = 0, Z = 3 }
                }
            });

            // 방2와 방1 사이 내벽 (방2 쪽)
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room2_WestWall",
                SurfaceType = "Wall",
                ConstructionName = "InteriorWall",
                ZoneName = "Room2",
                OutsideBoundaryCondition = "Surface",
                OutsideBoundaryConditionObject = "Room1_EastWall",
                SunExposure = "NoSun",
                WindExposure = "NoWind",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 4, Y = 3, Z = 3 },
                    new Vertex { X = 4, Y = 3, Z = 0 },
                    new Vertex { X = 4, Y = 0, Z = 0 },
                    new Vertex { X = 4, Y = 0, Z = 3 }
                }
            });

            // 방2 동쪽 벽
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room2_EastWall",
                SurfaceType = "Wall",
                ConstructionName = "ExteriorWall",
                ZoneName = "Room2",
                OutsideBoundaryCondition = "Outdoors",
                SunExposure = "SunExposed",
                WindExposure = "WindExposed",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 9, Y = 0, Z = 3 },
                    new Vertex { X = 9, Y = 0, Z = 0 },
                    new Vertex { X = 9, Y = 3, Z = 0 },
                    new Vertex { X = 9, Y = 3, Z = 3 }
                }
            });

            // 방2 남쪽 벽
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room2_SouthWall",
                SurfaceType = "Wall",
                ConstructionName = "ExteriorWall",
                ZoneName = "Room2",
                OutsideBoundaryCondition = "Outdoors",
                SunExposure = "SunExposed",
                WindExposure = "WindExposed",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 4, Y = 0, Z = 3 },
                    new Vertex { X = 4, Y = 0, Z = 0 },
                    new Vertex { X = 9, Y = 0, Z = 0 },
                    new Vertex { X = 9, Y = 0, Z = 3 }
                }
            });

            // 방2 북쪽 벽
            model.BuildingSurfaceDetaileds.Add(new BuildingSurfaceDetailed
            {
                Name = "Room2_NorthWall",
                SurfaceType = "Wall",
                ConstructionName = "ExteriorWall",
                ZoneName = "Room2",
                OutsideBoundaryCondition = "Outdoors",
                SunExposure = "SunExposed",
                WindExposure = "WindExposed",
                Vertices = new List<Vertex>
                {
                    new Vertex { X = 9, Y = 3, Z = 3 },
                    new Vertex { X = 9, Y = 3, Z = 0 },
                    new Vertex { X = 4, Y = 3, Z = 0 },
                    new Vertex { X = 4, Y = 3, Z = 3 }
                }
            });
        }

        static void CreateInternalLoads(EnergyPlusModel model)
        {
            // 방1 재실 (2명)
            model.People.Add(new People
            {
                Name = "Room1_People",
                ZoneName = "Room1",
                NumberofPeopleScheduleName = "OccupancySchedule",
                NumberofPeopleCalculationMethod = "People",
                NumberofPeople = 2,
                FractionRadiant = 0.3,
                SensibleHeatFraction = "autocalculate",
                ActivityLevelScheduleName = "AlwaysOn", // 활동 수준 120W/person 가정
                CarbonDioxideGenerationRate = 0.0000000382
            });

            // 방2 재실 (3명)
            model.People.Add(new People
            {
                NodeName = "Room2_People",
                ZoneName = "Room2",
                NumberofPeopleScheduleName = "OccupancySchedule",
                NumberofPeopleCalculationMethod = "People",
                NumberofPeople = 3,
                FractionRadiant = 0.3,
                SensibleHeatFraction = "autocalculate",
                ActivityLevelScheduleName = "AlwaysOn",
                CarbonDioxideGenerationRate = 0.0000000382
            });

            // 방1 조명 (10 W/m²)
            model.Lights.Add(new Lights
            {
                NodeName = "Room1_Lights",
                ZoneName = "Room1",
                ScheduleName = "LightingSchedule",
                DesignLevelCalculationMethod = "Watts/Area",
                WattsperZoneFloorArea = 10.0, // 10 W/m²
                ReturnAirFraction = 0.0,
                FractionRadiant = 0.42,
                FractionVisible = 0.18,
                FractionReplaceable = 1.0
            });

            // 방2 조명 (10 W/m²)
            model.Lights.Add(new Lights
            {
                Name = "Room2_Lights",
                ZoneName = "Room2",
                ScheduleName = "LightingSchedule",
                DesignLevelCalculationMethod = "Watts/Area",
                WattsperZoneFloorArea = 10.0,
                ReturnAirFraction = 0.0,
                FractionRadiant = 0.42,
                FractionVisible = 0.18,
                FractionReplaceable = 1.0
            });

            // 방1 사무기기 (8 W/m²)
            model.ElectricEquipment.Add(new ElectricEquipment
            {
                Name = "Room1_Equipment",
                ZoneName = "Room1",
                ScheduleName = "EquipmentSchedule",
                DesignLevelCalculationMethod = "Watts/Area",
                WattsperZoneFloorArea = 8.0, // 8 W/m²
                FractionLatent = 0.0,
                FractionRadiant = 0.3,
                FractionLost = 0.0
            });

            // 방2 사무기기 (8 W/m²)
            model.ElectricEquipment.Add(new ElectricEquipment
            {
                Name = "Room2_Equipment",
                ZoneName = "Room2",
                ScheduleName = "EquipmentSchedule",
                DesignLevelCalculationMethod = "Watts/Area",
                WattsperZoneFloorArea = 8.0,
                FractionLatent = 0.0,
                FractionRadiant = 0.3,
                FractionLost = 0.0
            });
        }

        static void CreateHVACSystem(EnergyPlusModel model)
        {
            // 방1 Ideal Loads Air System
            model.HVACTemplateZoneIdealLoadsAirSystems.Add(new HVACTemplateZoneIdealLoadsAirSystem
            {
                ZoneName = "Room1",
                TemplateThermo statName = "",
                SystemAvailabilityScheduleName = "AlwaysOn",
                MaximumHeatingSupplyAirTemperature = 50.0,
                MinimumCoolingSupplyAirTemperature = 13.0,
                MaximumHeatingSupplyAirHumidityRatio = 0.0156,
                MinimumCoolingSupplyAirHumidityRatio = 0.0077,
                HeatingLimit = "NoLimit",
                CoolingLimit = "NoLimit",
                DehumidificationControlType = "None",
                CoolingSensibleHeatRatio = 0.7,
                HumidificationControlType = "None",
                DesignSpecificationOutdoorAirObjectName = "",
                OutdoorAirInletNodeName = "",
                DemandControlledVentilationType = "None",
                OutdoorAirEconomizerType = "NoEconomizer",
                HeatRecoveryType = "None"
            });

            // 방2 Ideal Loads Air System
            model.HVACTemplateZoneIdealLoadsAirSystems.Add(new HVACTemplateZoneIdealLoadsAirSystem
            {
                ZoneName = "Room2",
                TemplateThermo statName = "",
                SystemAvailabilityScheduleName = "AlwaysOn",
                MaximumHeatingSupplyAirTemperature = 50.0,
                MinimumCoolingSupplyAirTemperature = 13.0,
                MaximumHeatingSupplyAirHumidityRatio = 0.0156,
                MinimumCoolingSupplyAirHumidityRatio = 0.0077,
                HeatingLimit = "NoLimit",
                CoolingLimit = "NoLimit",
                DehumidificationControlType = "None",
                CoolingSensibleHeatRatio = 0.7,
                HumidificationControlType = "None",
                DesignSpecificationOutdoorAirObjectName = "",
                OutdoorAirInletNodeName = "",
                DemandControlledVentilationType = "None",
                OutdoorAirEconomizerType = "NoEconomizer",
                HeatRecoveryType = "None"
            });

            // 방1 온도조절기 (20-24°C)
            model.ThermostatSetpointDualSetpoints.Add(new ThermostatSetpointDualSetpoint
            {
                Name = "Room1_Thermostat",
                HeatingSetpointTemperatureScheduleName = "HeatingSetpoint",
                CoolingSetpointTemperatureScheduleName = "CoolingSetpoint"
            });

            // 방2 온도조절기 (20-24°C)
            model.ThermostatSetpointDualSetpoints.Add(new ThermostatSetpointDualSetpoint
            {
                Name = "Room2_Thermostat",
                HeatingSetpointTemperatureScheduleName = "HeatingSetpoint",
                CoolingSetpointTemperatureScheduleName = "CoolingSetpoint"
            });

            // 난방 설정점 스케줄 (20°C)
            model.ScheduleCompacts.Add(new ScheduleCompact
            {
                Name = "HeatingSetpoint",
                ScheduleTypeLimitsName = "Temperature",
                FieldSet = new List<string>
                {
                    "Through: 12/31",
                    "For: AllDays",
                    "Until: 24:00", "20.0"
                }
            });

            // 냉방 설정점 스케줄 (24°C)
            model.ScheduleCompacts.Add(new ScheduleCompact
            {
                Name = "CoolingSetpoint",
                ScheduleTypeLimitsName = "Temperature",
                FieldSet = new List<string>
                {
                    "Through: 12/31",
                    "For: AllDays",
                    "Until: 24:00", "24.0"
                }
            });

            // Zone에 온도조절기 연결
            model.ZoneControls_Thermostats.Add(new ZoneControl_Thermostat
            {
                Name = "Room1_ThermostatControl",
                ZoneName = "Room1",
                ControlTypeScheduleName = "AlwaysOn",
                Control1ObjectType = "ThermostatSetpoint:DualSetpoint",
                Control1Name = "Room1_Thermostat"
            });

            model.ZoneControls_Thermostats.Add(new ZoneControl_Thermostat
            {
                Name = "Room2_ThermostatControl",
                ZoneName = "Room2",
                ControlTypeScheduleName = "AlwaysOn",
                Control1ObjectType = "ThermostatSetpoint:DualSetpoint",
                Control1Name = "Room2_Thermostat"
            });
        }

        static void CreateOutputs(EnergyPlusModel model)
        {
            // 출력 변수 정의
            model.OutputVariables.Add(new OutputVariable
            {
                KeyValue = "*",
                VariableName = "Zone Mean Air Temperature",
                ReportingFrequency = "Hourly"
            });

            model.OutputVariables.Add(new OutputVariable
            {
                KeyValue = "*",
                VariableName = "Zone Ideal Loads Zone Total Heating Energy",
                ReportingFrequency = "Hourly"
            });

            model.OutputVariables.Add(new OutputVariable
            {
                KeyValue = "*",
                VariableName = "Zone Ideal Loads Zone Total Cooling Energy",
                ReportingFrequency = "Hourly"
            });

            model.OutputVariables.Add(new OutputVariable
            {
                KeyValue = "*",
                VariableName = "Zone Lights Electric Energy",
                ReportingFrequency = "Hourly"
            });

            model.OutputVariables.Add(new OutputVariable
            {
                KeyValue = "*",
                VariableName = "Zone Electric Equipment Electric Energy",
                ReportingFrequency = "Hourly"
            });

            // 표준 보고서
            model.OutputTableSummaryReports.Add(new OutputTableSummaryReports
            {
                Reports = new List<string>
                {
                    "AllSummary",
                    "AllMonthly"
                }
            });
        }

        static void SaveToFile(EnergyPlusModel model, string filename)
        {
            // IDF 형식으로 저장
            var idfSerializer = new IDFSerializer();
            idfSerializer.Serialize(model, filename);
        }
    }

    // 간단한 Vertex 클래스
    public class Vertex
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
}