using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Codebound.Gameplay;
using Codebound.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Codebound.Tests.PlayMode
{
    public class PlayerActorTests
    {
        private static readonly LevelLayout Layout = new LevelLayout
        {
            Width = 6,
            Height = 3,
            Start = new GridPosition(1, 1),
            Terminal = new GridPosition(4, 1),
        };

        private GameObject character;
        private PlayerActor actor;

        [SetUp]
        public void CreateCharacter()
        {
            character = new GameObject("Character");
            actor = character.AddComponent<PlayerActor>();
            actor.SecondsPerStep = 0.01f;
            actor.PlaceAt(Layout.Start);
        }

        [TearDown]
        public void DestroyCharacter()
        {
            Object.Destroy(character);
        }

        [UnityTest]
        public IEnumerator TheCharacterEndsWhereTheProgramLeftIt()
        {
            var run = LevelRunner.Run("MoveRight();\nMoveRight();\nMoveDown();", Layout);

            yield return actor.Play(run.Steps);

            Assert.That(character.transform.localPosition, Is.EqualTo(new Vector3(3f, -2f, 0f)));
        }

        [UnityTest]
        public IEnumerator AStepThatFailsLeavesTheCharacterInPlace()
        {
            var run = LevelRunner.Run("MoveUp();\nMoveUp();", Layout);

            yield return actor.Play(run.Steps);

            Assert.That(run.Steps[1].Succeeded, Is.False);
            Assert.That(character.transform.localPosition, Is.EqualTo(new Vector3(1f, 0f, 0f)));
        }

        [UnityTest]
        public IEnumerator EveryStepIsAnnouncedInProgramOrder()
        {
            var run = LevelRunner.Run("MoveRight();\nMoveRight();\nInteract();", Layout);
            var lines = new List<int>();
            actor.StepStarted += step => lines.Add(step.Line);

            yield return actor.Play(run.Steps);

            Assert.That(lines, Is.EqualTo(run.Steps.Select(step => step.Line)));
            Assert.That(lines, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [UnityTest]
        public IEnumerator PlacingTheCharacterUsesTheGridCell()
        {
            actor.PlaceAt(new GridPosition(4, 2));
            yield return null;

            Assert.That(character.transform.localPosition, Is.EqualTo(new Vector3(4f, -2f, 0f)));
        }
    }
}
