using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using LibreHardwareMonitor.Hardware;

public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var subHardware in hardware.SubHardware) subHardware.Accept(this);
    }

    public void VisitSensor(ISensor sensor)
    {
    }

    public void VisitParameter(IParameter parameter)
    {
    }
}

internal class Program
{
    private static void Main(string[] args)
    {
        var computer = new Computer
        {
            IsGpuEnabled = true,
            IsMemoryEnabled = true
        };

        computer.Open();

        var memory = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Memory);
        var gpu = computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia);
        if (gpu == null)
            gpu = computer.Hardware.FirstOrDefault(h =>
                h.HardwareType == HardwareType.GpuAmd || h.HardwareType == HardwareType.GpuIntel);
        var gpuName = gpu?.Name;
        var gpuType = gpu?.HardwareType.ToString().Replace("Gpu", "");

        while (true)
        {
            computer.Accept(new UpdateVisitor());

            float usedMemory = -1, availableMemory = -1, totalMemory = -1;
            float gpuUsage = -1, usedVram = -1, totalVram = -1;

            if (memory != null)
                foreach (var s in memory.Sensors)
                {
                    if (s.Name == "Memory Used")
                        usedMemory = s.Value.GetValueOrDefault();
                    else if (s.Name == "Memory Available")
                        availableMemory = s.Value.GetValueOrDefault();
                    if (usedMemory != -1 && availableMemory != -1)
                    {
                        totalMemory = usedMemory + availableMemory;
                        break;
                    }
                }

            if (gpu != null)
                foreach (var s in gpu.Sensors)
                {
                    if (s.Name == "GPU Core" && s.SensorType == SensorType.Load)
                        gpuUsage = s.Value.GetValueOrDefault();
                    else if (s.Name == "GPU Memory Used")
                        usedVram = s.Value.GetValueOrDefault();
                    else if (s.Name == "GPU Memory Total")
                        totalVram = s.Value.GetValueOrDefault();
                    if (gpuUsage != -1 && usedVram != -1 && totalVram != -1) break;
                }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                gpuType,
                gpuName,
                usedMemory,
                totalMemory,
                gpuUsage,
                usedVram,
                totalVram
            }));

            Thread.Sleep(750);
        }
    }
}
