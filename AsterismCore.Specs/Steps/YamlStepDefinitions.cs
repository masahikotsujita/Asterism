using TechTalk.SpecFlow;
using AsterismCore;
using FluentAssertions;

namespace AsterismCore.Specs.Steps {
    [Binding]
    public sealed class YamlStepDefinitions {

        // For additional details on SpecFlow step definitions see https://go.specflow.org/doc-stepdef

        private readonly ScenarioContext _scenarioContext;

        private Yaml _yaml;

        public YamlStepDefinitions(ScenarioContext scenarioContext) {
            _scenarioContext = scenarioContext;
        }

        [Given("empty yaml")]
        public void GivenEmptyYaml() {
            _yaml = new Yaml();
        }

        [Then("elements are null")]
        public void ThenElementsAreNull() {
            _yaml.GetStringOrDefault().Should().BeNull();
            _yaml["path"]["to"]["some"]["value"].GetStringOrDefault().Should().BeNull();
            _yaml["path"]["to"]["some"]["values"].GetListOrDefault().Should().BeNull();
            _yaml["path"]["to"]["some"]["values"][1].GetStringOrDefault().Should().BeNull();
            _yaml["path"]["to"]["some"]["values"][100].GetStringOrDefault().Should().BeNull();
        }
    }
}
