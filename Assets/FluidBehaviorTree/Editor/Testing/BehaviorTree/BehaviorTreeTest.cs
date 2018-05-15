using System;
using Adnc.FluidBT.Tasks;
using Adnc.FluidBT.Tasks.Actions;
using Adnc.FluidBT.Trees;
using NSubstitute;
using NUnit.Framework;

namespace Adnc.FluidBT.Testing {
    public class BehaviorTreeTests {
        BehaviorTree tree;

        public interface INodeAwakeExample : ITask, IEventAwake {
        }

        [SetUp]
        public void SetBehaviorTree () {
            tree = new BehaviorTree();
        }

        public class OnInit : BehaviorTreeTests {
            [Test]
            public void It_should_initialize () {
                Assert.IsNotNull(tree);
            }

            [Test]
            public void It_should_set_the_root_node_as_current_by_default () {
                Assert.AreEqual(tree.Root, tree.Current);
            }
        }

        public class AwakeEvent : BehaviorTreeTests {
            [Test]
            public void Trigger_on_all_nodes_with_the_awake_interface () {
                var node = Substitute.For<INodeAwakeExample>();
                node.Enabled.Returns(true);

                tree.AddNode(tree.Root, node);
                tree.Setup();

                node.Received().Awake();
            }

            [Test]
            public void Calling_setup_again_should_not_refire_awake () {
                var node = Substitute.For<INodeAwakeExample>();
                node.Enabled.Returns(true);

                tree.AddNode(tree.Root, node);
                tree.Setup();
                tree.Setup();

                node.Received(1).Awake();
            }
        }

        public class AddNodeMethod : BehaviorTreeTests {
            private ITask action;

            [SetUp]
            public void SetDefaultAction () {
                action = Substitute.For<INodeAwakeExample>();
                action.Enabled.Returns(true);
            }

            public class AddNodeMethodSuccess : AddNodeMethod {
                [SetUp]
                public void AddNode () {
                    tree.AddNode(tree.Root, action);

                    Assert.AreEqual(tree.Root.children[0], action);
                }

                [Test]
                public void Parent_node_is_added_to_the_child () {
                    Assert.AreEqual(action, tree.Root.children[0]);
                }

                [Test]
                public void Add_child_node_to_node_list () {
                    Assert.Contains(action, tree.nodeAwake);
                }

                [Test]
                public void Add_child_node_to_nodes () {
                    Assert.IsTrue(tree.nodes.Contains(action));
                }

                [Test]
                public void Attaches_a_reference_to_the_behavior_tree () {
                    action.Received().Owner = tree;
                }
            }

            public class AddNodeMethodError : AddNodeMethod {
                [Test]
                public void Error_if_parent_is_null () {
                    Assert.Throws<ArgumentNullException>(
                        () => tree.AddNode(null, action),
                        "Parent cannot be null");
                }

                [Test]
                public void Error_if_child_is_null () {
                    Assert.Throws<ArgumentNullException>(
                        () => tree.AddNode(tree.Root, null),
                        "Child cannot be null");
                }
            }
        }

        public class TickMethod {
            public class WithSingleChild : BehaviorTreeTests {
                [Test]
                public void Update_the_first_child_task_on_update () {
                    var action = A.TaskStub().Build();
                    tree.AddNode(tree.Root, action);

                    tree.Update();

                    action.Received(1).Update();
                }

                [Test]
                public void Update_the_first_child_task_on_update_multiple_times_if_continue_status () {
                    var action = A.TaskStub().WithUpdateStatus(TaskStatus.Continue).Build();
                    tree.AddNode(tree.Root, action);

                    tree.Update();
                    tree.Update();

                    action.Received(2).Update();
                }

                [Test]
                public void Stops_ticking_after_tree_is_finished () {
                    var action = A.TaskStub().Build();
                    tree.AddNode(tree.Root, action);

                    tree.Update();
                    tree.Update();

                    action.Received(1).Update();
                }

                [Test]
                public void Continues_ticking_after_tree_is_finished_if_repeat_is_true () {
                    var action = A.TaskStub().Build();
                    tree.repeat = true;
                    tree.AddNode(tree.Root, action);

                    tree.Update();
                    tree.Update();

                    action.Received(2).Update();
                }

                public void Does_not_call_reset_when_tree_is_finished () {
                }

                public void Calls_reset_when_tree_is_finished_if_repeat_is_true () {
                }

                public void Only_calls_reset_when_reset_has_been_called () {
                    // Still need to call reset when a conditional abort triggers
                }

                public class GenericAction : BehaviorTreeTests {
                    private ITask RunAction (TaskStatus status) {
                        var action = new ActionGeneric { updateLogic = () => status };
                        tree.AddNode(tree.Root, action);
                        tree.Update();

                        return action;
                    }

                    [Test]
                    public void Update_sets_action_as_current_on_action_status_success () {
                        Assert.AreEqual(RunAction(TaskStatus.Success), tree.Current);
                    }

                    [Test]
                    public void Update_sets_action_as_current_on_status_failure () {
                        Assert.AreEqual(RunAction(TaskStatus.Failure), tree.Current);
                    }

                    [Test]
                    public void Update_sets_action_as_current_on_status_continue () {
                        Assert.AreEqual(RunAction(TaskStatus.Continue), tree.Current);
                    }
                }
            }

            public class WithSequence : TickMethod {
                public void Ticks_where_a_continue_left_off_from () {
                }

                public void Does_not_continue_ticking_if_tree_is_exhausted () {
                }
            }
        }
    }
}