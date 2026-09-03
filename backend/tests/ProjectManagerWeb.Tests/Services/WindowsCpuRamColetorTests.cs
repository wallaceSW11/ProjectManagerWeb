using LibreHardwareMonitor.Hardware;
using ProjectManagerWeb.src.Services.Monitoramento.Coletores;

namespace ProjectManagerWeb.Tests.Services;

public class WindowsCpuRamColetorTests
{
    private static IHardware CriarHardware(HardwareType tipo, params ISensor[] sensores)
    {
        var hardware = Substitute.For<IHardware>();
        hardware.HardwareType.Returns(tipo);
        hardware.Sensors.Returns(sensores);
        return hardware;
    }

    private static ISensor CriarSensor(SensorType tipo, string nome, float? valor)
    {
        var sensor = Substitute.For<ISensor>();
        sensor.SensorType.Returns(tipo);
        sensor.Name.Returns(nome);
        sensor.Value.Returns(valor);
        return sensor;
    }

    public class ObterMaiorTemperaturaCpu : WindowsCpuRamColetorTests
    {
        [Fact]
        public void Deve_retornar_maior_temperatura_entre_sensores_de_multiplos_hardwares_cpu()
        {
            var hardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Temperature, "Core #1", 55.0f),
                CriarSensor(SensorType.Temperature, "Core #2", 72.5f));
            var outroHardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Temperature, "Core #3", 64.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaCpu(new[] { hardwareCpu, outroHardwareCpu });

            resultado.Should().Be(72.5);
        }

        [Fact]
        public void Deve_ignorar_hardware_que_nao_eh_cpu()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "Composite Temperature", 60.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaCpu(new[] { hardwareStorage });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_unico_sensor_com_temperatura_zero()
        {
            var hardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Temperature, "CPU Package", 0.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaCpu(new[] { hardwareCpu });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_unico_sensor_com_temperatura_acima_de_150()
        {
            var hardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Temperature, "CPU Package", 151.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaCpu(new[] { hardwareCpu });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_unico_sensor_sem_valor()
        {
            var hardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Temperature, "CPU Package", null));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaCpu(new[] { hardwareCpu });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_lista_vazia()
        {
            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaCpu(Array.Empty<IHardware>());

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_ignorar_sensor_que_nao_eh_de_temperatura()
        {
            var hardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Load, "CPU Total", 45.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaCpu(new[] { hardwareCpu });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_unico_sensor_com_valor_nao_finito()
        {
            var hardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Temperature, "CPU Package", float.NaN));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaCpu(new[] { hardwareCpu });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_maior_temperatura_ignorando_sensores_invalidos()
        {
            var hardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Temperature, "Core #1", 0.0f),
                CriarSensor(SensorType.Temperature, "Core #2", 170.0f),
                CriarSensor(SensorType.Temperature, "Core #3", null),
                CriarSensor(SensorType.Temperature, "Core #4", 68.0f),
                CriarSensor(SensorType.Load, "CPU Total", 30.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaCpu(new[] { hardwareCpu });

            resultado.Should().Be(68.0);
        }
    }

    public class ObterMaiorTemperaturaDisco : WindowsCpuRamColetorTests
    {
        [Fact]
        public void Deve_retornar_maior_temperatura_entre_sensores_de_multiplos_hardwares_storage()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "Composite Temperature", 42.0f));
            var outroHardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "Drive #1", 38.0f),
                CriarSensor(SensorType.Temperature, "Drive #2", 47.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareStorage, outroHardwareStorage });

            resultado.Should().Be(47.0);
        }

        [Fact]
        public void Deve_ignorar_sensores_warning_e_critical_e_retornar_composite()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "Warning Temperature", 40.0f),
                CriarSensor(SensorType.Temperature, "Critical Temperature", 45.0f),
                CriarSensor(SensorType.Temperature, "Composite Temperature", 48.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareStorage });

            resultado.Should().Be(48.0);
        }

        [Fact]
        public void Deve_retornar_null_quando_apenas_sensores_warning_e_critical()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "Warning Temperature", 40.0f),
                CriarSensor(SensorType.Temperature, "Critical Temperature", 45.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareStorage });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_ignorar_sensor_warning_em_caixa_diferente()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "WARNING TEMPERATURE", 40.0f),
                CriarSensor(SensorType.Temperature, "Composite Temperature", 35.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareStorage });

            resultado.Should().Be(35.0);
        }

        [Fact]
        public void Deve_ignorar_hardware_que_nao_eh_storage()
        {
            var hardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Temperature, "CPU Package", 55.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareCpu });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_unico_sensor_sem_valor()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "Composite Temperature", null));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareStorage });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_unico_sensor_com_temperatura_acima_de_120()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "Composite Temperature", 121.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareStorage });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_lista_vazia()
        {
            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(Array.Empty<IHardware>());

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_ignorar_sensor_que_nao_eh_de_temperatura()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Load, "Drive #1 Load", 50.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareStorage });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_unico_sensor_com_valor_nao_finito()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "Composite Temperature", float.PositiveInfinity));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareStorage });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_maior_temperatura_ignorando_sensores_invalidos()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Temperature, "Warning Temperature", 40.0f),
                CriarSensor(SensorType.Temperature, "Composite Temperature", 130.0f),
                CriarSensor(SensorType.Temperature, "Drive #1", null),
                CriarSensor(SensorType.Temperature, "Drive #2", 52.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorTemperaturaDisco(new[] { hardwareStorage });

            resultado.Should().Be(52.0);
        }
    }

    public class ObterMaiorRotacaoCooler : WindowsCpuRamColetorTests
    {
        [Fact]
        public void Deve_retornar_maior_rpm_entre_fans_de_motherboard_e_cpu()
        {
            var hardwareMotherboard = CriarHardware(HardwareType.Motherboard,
                CriarSensor(SensorType.Fan, "Fan #1", 1200.0f),
                CriarSensor(SensorType.Fan, "Fan #2", 1400.0f));
            var hardwareCpu = CriarHardware(HardwareType.Cpu,
                CriarSensor(SensorType.Fan, "CPU Fan", 1300.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorRotacaoCooler(new[] { hardwareMotherboard, hardwareCpu });

            resultado.Should().Be(1400.0);
        }

        [Fact]
        public void Deve_ignorar_hardware_que_nao_eh_motherboard_nem_cpu()
        {
            var hardwareStorage = CriarHardware(HardwareType.Storage,
                CriarSensor(SensorType.Fan, "Fan #1", 3000.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorRotacaoCooler(new[] { hardwareStorage });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_ignorar_sensor_que_nao_eh_fan()
        {
            var hardwareMotherboard = CriarHardware(HardwareType.Motherboard,
                CriarSensor(SensorType.Temperature, "Motherboard", 60.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorRotacaoCooler(new[] { hardwareMotherboard });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_ignorar_valores_nulos_e_menores_ou_iguais_a_zero()
        {
            var hardwareMotherboard = CriarHardware(HardwareType.Motherboard,
                CriarSensor(SensorType.Fan, "Fan #1", null),
                CriarSensor(SensorType.Fan, "Fan #2", 0.0f),
                CriarSensor(SensorType.Fan, "Fan #3", -5.0f),
                CriarSensor(SensorType.Fan, "Fan #4", 900.0f));

            var resultado = WindowsCpuRamColetor.ObterMaiorRotacaoCooler(new[] { hardwareMotherboard });

            resultado.Should().Be(900.0);
        }

        [Fact]
        public void Deve_ignorar_valores_nao_finitos()
        {
            var hardwareMotherboard = CriarHardware(HardwareType.Motherboard,
                CriarSensor(SensorType.Fan, "Fan #1", float.NaN),
                CriarSensor(SensorType.Fan, "Fan #2", float.PositiveInfinity));

            var resultado = WindowsCpuRamColetor.ObterMaiorRotacaoCooler(new[] { hardwareMotherboard });

            resultado.Should().BeNull();
        }

        [Fact]
        public void Deve_retornar_null_quando_lista_vazia()
        {
            var resultado = WindowsCpuRamColetor.ObterMaiorRotacaoCooler(Array.Empty<IHardware>());

            resultado.Should().BeNull();
        }
    }
}
